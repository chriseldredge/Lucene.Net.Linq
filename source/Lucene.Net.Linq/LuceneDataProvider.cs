using System;
using System.Linq;
using Common.Logging;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
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
    public class LuceneDataProvider
    {
        private static readonly ILog Log = LogManager.GetLogger<LuceneDataProvider>();

        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private readonly IQueryParser queryParser;
        private readonly Context context;

        /// <summary>
        /// Constructs a new read-only instance without supplying an IndexWriter.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="analyzer"></param>
        /// <param name="version"></param>
        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version)
            : this(directory, analyzer, version, null, new object())
        {
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="analyzer"></param>
        /// <param name="version"></param>
        /// <param name="indexWriter"></param>
        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version, IndexWriter indexWriter)
            : this(directory, analyzer, version, new IndexWriterAdapter(indexWriter), new object())
        {
        }

        /// <summary>
        /// If the supplied IndexWriter will be written to outside of this instance of LuceneDataProvider,
        /// the <paramref name="transactionLock"/> will be used to coordinate writes.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="analyzer"></param>
        /// <param name="version"></param>
        /// <param name="indexWriter"></param>
        /// <param name="transactionLock"></param>
        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version, IIndexWriter indexWriter, object transactionLock)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;

            queryParser = RelinqQueryParserFactory.CreateQueryParser();
            context = new Context(this.directory, this.analyzer, this.version, indexWriter, transactionLock);
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
            return AsQueryable(factory, new ReflectionDocumentMapper<T>());
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
            return OpenSession(factory, new ReflectionDocumentMapper<T>());
        }
        /// <summary>
        /// Opens a session for staging changes and then committing them atomically.
        /// </summary>
        /// <param name="factory">Factory delegate that creates new instances of <typeparamref name="T"/></param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        /// <typeparam name="T">The type of object that will be mapped to <c cref="Document"/>.</typeparam>
        public ISession<T> OpenSession<T>(Func<T> factory, IDocumentMapper<T> documentMapper)
        {
            if (context.IsReadOnly)
            {
                throw new InvalidOperationException("This data provider is read-only. To enable writes, construct " + typeof(LuceneDataProvider) + " with an IndexWriter.");
            }

            return new LuceneSession<T>(documentMapper, context, CreateQueryable(factory, context, documentMapper));
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
            RegisterCacheWarmingCallback(callback, factory, new ReflectionDocumentMapper<T>());
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

        private LuceneQueryable<T> CreateQueryable<T>(Func<T> factory, Context context, IDocumentMapper<T> mapper)
        {
            var executor = new LuceneQueryExecutor<T>(context, factory, mapper);
            return new LuceneQueryable<T>(queryParser, executor);
        }

        private class WarmUpContext : Context
        {
            private readonly IndexSearcher newSearcher;

            internal WarmUpContext(Context target, IndexSearcher newSearcher)
                : base(target.Directory, target.Analyzer, target.Version, target.IndexWriter, target.TransactionLock)
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
    }
}
