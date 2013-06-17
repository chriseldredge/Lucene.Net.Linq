using System;
using System.Linq;
using Common.Logging;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Remotion.Linq.Parsing.Structure;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Provides IQueryable access to a Lucene.Net index as well as an API
    /// for adding, deleting and replacing documents within atomic transactions.
    /// </summary>
    public class LuceneDataProvider : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger<LuceneDataProvider>();

        private readonly Directory directory;
        private readonly Analyzer externalAnalyzer;
        private readonly PerFieldAnalyzer perFieldAnalyzer;
        private readonly Version version;
        private readonly object sync = new object();
        private readonly IQueryParser queryParser;
        private readonly Context context;
        private readonly bool writerIsExternal;

        private IIndexWriter writer;

        /// <summary>
        /// Constructs a new instance with a client-provided <see cref="Analyzer"/>
        /// </summary>
        public LuceneDataProvider(Directory directory, Analyzer externalAnalyzer, Version version)
            : this(directory, externalAnalyzer, version, null, new object())
        {
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public LuceneDataProvider(Directory directory, Version version)
            : this(directory, null, version, null, new object())
        {
        }

        /// <summary>
        /// Constructs a new instance with an externally provided <see cref="IndexWriter"/>
        /// </summary>
        public LuceneDataProvider(Directory directory, Version version, IndexWriter externalWriter)
            : this(directory, null, version, new IndexWriterAdapter(externalWriter), new object())
        {
        }

        /// <summary>
        /// Constructs a new instance with a client-provided <see cref="Analyzer"/> and <see cref="IndexWriter"/>
        /// </summary>
        public LuceneDataProvider(Directory directory, Analyzer externalAnalyzer, Version version, IndexWriter indexWriter)
            : this(directory, externalAnalyzer, version, new IndexWriterAdapter(indexWriter), new object())
        {
        }

        /// <summary>
        /// Constructs a new instance with a client provided <see cref="Analyzer"/>.
        /// If the supplied IndexWriter will be written to outside of this instance of LuceneDataProvider,
        /// the <paramref name="transactionLock"/> will be used to coordinate writes.
        /// </summary>
        public LuceneDataProvider(Directory directory, Version version, IIndexWriter externalWriter, object transactionLock)
            : this(directory, null, version, externalWriter, transactionLock)
        {
        }

        /// <summary>
        /// Constructs a new instance.
        /// If the supplied IndexWriter will be written to outside of this instance of LuceneDataProvider,
        /// the <paramref name="transactionLock"/> will be used to coordinate writes.
        /// </summary>
        public LuceneDataProvider(Directory directory, Analyzer externalAnalyzer, Version version, IIndexWriter externalWriter, object transactionLock)
        {
            this.directory = directory;
            this.externalAnalyzer = externalAnalyzer;
            this.perFieldAnalyzer = new PerFieldAnalyzer(new KeywordAnalyzer());
            this.version = version;
            this.writerIsExternal = externalWriter != null;
            this.writer = externalWriter ?? IndexWriter;

            queryParser = RelinqQueryParserFactory.CreateQueryParser();
            context = new Context(this.directory, transactionLock);
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(System.Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>() where T : new()
        {
            return AsQueryable(() => new T());
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(System.Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(IDocumentMapper<T> documentMapper) where T : new()
        {
            return AsQueryable(() => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(System.Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(Func<T> factory)
        {
            return AsQueryable(factory, new ReflectionDocumentMapper<T>(version, externalAnalyzer));
        }

        /// <summary>
        /// Returns an IQueryable implementation where the type being mapped
        /// from <c cref="Document"/> is constructed by a factory delegate.
        /// </summary>
        /// <typeparam name="T">The type of object that Document will be mapped onto.</typeparam>
        /// <param name="factory">Factory method to instantiate new instances of T.</param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        public IQueryable<T> AsQueryable<T>(Func<T> factory, IDocumentMapper<T> documentMapper)
        {
            return CreateQueryable(factory, context, documentMapper);
        }

        /// <summary>
        /// <see cref="OpenSession{T}(Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>() where T : new()
        {
            return OpenSession(() => new T());
        }

        /// <summary>
        /// <see cref="OpenSession{T}(Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(IDocumentMapper<T> documentMapper) where T : new()
        {
            return OpenSession(() => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="OpenSession{T}(Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(Func<T> factory)
        {
            return OpenSession(factory, new ReflectionDocumentMapper<T>(version, externalAnalyzer));
        }

        /// <summary>
        /// Opens a session for staging changes and then committing them atomically.
        /// </summary>
        /// <param name="factory">Factory delegate that creates new instances of <typeparamref name="T"/></param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        /// <typeparam name="T">The type of object that will be mapped to <c cref="Document"/>.</typeparam>
        public ISession<T> OpenSession<T>(Func<T> factory, IDocumentMapper<T> documentMapper)
        {
            perFieldAnalyzer.Merge(documentMapper.Analyzer);

            return new LuceneSession<T>(
                documentMapper,
                IndexWriter,
                context,
                CreateQueryable(factory, context, documentMapper));
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback) where T : new()
        {
            RegisterCacheWarmingCallback(callback, () => new T());
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, IDocumentMapper<T> documentMapper) where T : new()
        {
            RegisterCacheWarmingCallback(callback, () => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, Func{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, Func<T> factory)
        {
            RegisterCacheWarmingCallback(callback, factory, new ReflectionDocumentMapper<T>(version, null));
        }

        /// <summary>
        /// Registers a callback to be invoked when a new IndexSearcher is being initialized.
        /// This method allows an IndexSearcher to be "warmed up" by executing one or more
        /// queries before the instance becomes visible on other threads.
        /// 
        /// While callbacks are being executed, other threads will continue to use the previous
        /// instance of IndexSearcher if this is not the first instance being initialized.
        /// 
        /// If this is the first instance, other threads will block until all callbacks complete.
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, Func<T> factory, IDocumentMapper<T> documentMapper)
        {
            context.SearcherLoading += (s, e) =>
            {
                Log.Trace(m => m("Invoking cache warming callback " + factory));

                var warmupContext = new WarmUpContext(context, e.IndexSearcher);
                var queryable = CreateQueryable(factory, warmupContext, documentMapper);
                callback(queryable);

                Log.Trace(m => m("Callback {0} completed.", factory));
            };
        }

        /// <summary>
        /// Retrieves the instance of IndexWriter that will be used by all
        /// sessions created by this instance. If the current writer has
        /// been disposed or a rollback occurred, a new instance will be
        /// created, unless the instance was passed in as a constructor
        /// parameter.
        /// </summary>
        public IIndexWriter IndexWriter
        {
            get
            {
                lock(sync)
                {
                    if (writer != null && !writer.IsClosed) return writer;

                    if (writerIsExternal)
                    {
                        throw new InvalidOperationException("Externally created writer has been closed.");
                    }

                    writer = GetIndexWriter(perFieldAnalyzer);
                }

                return writer;
            }
        }

        public void Dispose()
        {
            if (writerIsExternal) return;
            
            IndexWriter.Dispose();
        }

        protected virtual IIndexWriter GetIndexWriter(Analyzer analyzer)
        {
            return new IndexWriterAdapter(new IndexWriter(directory, analyzer, ShouldCreateIndex, DeletionPolicy, MaxFieldLength));
        }

        protected virtual bool ShouldCreateIndex
        {
            get
            {
                try
                {
                    return !directory.ListAll().Any();
                }
                catch (NoSuchDirectoryException)
                {
                    return true;
                }
            }
        }

        protected virtual IndexDeletionPolicy DeletionPolicy
        {
            get { return new KeepOnlyLastCommitDeletionPolicy(); }
        }

        protected virtual IndexWriter.MaxFieldLength MaxFieldLength
        {
            get { return Index.IndexWriter.MaxFieldLength.UNLIMITED; }
        }

        private LuceneQueryable<T> CreateQueryable<T>(Func<T> factory, Context context, IDocumentMapper<T> mapper)
        {
            var executor = new LuceneQueryExecutor<T>(context, factory, mapper);
            return new LuceneQueryable<T>(queryParser, executor);
        }

        private class WarmUpContext : Context
        {
            private readonly IndexSearcher newSearcher;

            internal WarmUpContext(Context target, IndexSearcher newSearcher)
                : base(target.Directory, target.TransactionLock)
            {
                this.newSearcher = newSearcher;
            }

            protected override IndexSearcher CreateSearcher()
            {
                return newSearcher;
            }
        }

        internal Context Context
        {
            get
            {
                return context;
            }
        }

        internal PerFieldAnalyzer Analyzer
        {
            get { return perFieldAnalyzer; }
        }
    }
}
