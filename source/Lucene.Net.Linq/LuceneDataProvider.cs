﻿using System;
using System.Collections.Generic;
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
    /// Delegate used by <see cref="LuceneDataProvider"/> to create instances of objects that do not
    /// have public default constructors.
    /// </summary>
    /// <typeparam name="T">The type of object <see cref="Document"/>s will be mapped onto.</typeparam>
    /// <returns>An instance of <paramref name="T"/></returns>
    public delegate T ObjectFactory<out T>();

    /// <summary>
    /// Delegate used by <see cref="LuceneDataProvider"/> to create or reuse instances of objects
    /// for advanced clients that may wish to provide caching.
    /// </summary>
    /// <typeparam name="T">The type of object <see cref="Document"/>s will be mapped onto.</typeparam>
    /// <param name="key">A key that uniquely identifies the <see cref="Document"/>.</param>
    /// <returns>An instance of <paramref name="T"/></returns>
    public delegate T ObjectLookup<out T>(IDocumentKey key);

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

            queryParser = RelinqQueryParserFactory.CreateQueryParser();
            context = new Context(this.directory, transactionLock);

            writerIsExternal = externalWriter != null;
            writer = externalWriter ?? IndexWriter;
        }

        /// <summary>
        /// Settings that enable or disable optional behavior.
        /// </summary>
        public LuceneDataProviderSettings Settings
        {
            get { return context.Settings; }
            set { context.Settings = value; }
        }

        /// <summary>
        /// Create a <see cref="QueryParsers.QueryParser"/> suitable for parsing advanced queries
        /// that cannot not expressed as LINQ (e.g. queries submitted by a user).
        ///
        /// After the instance is returned, options such as <see cref="QueryParsers.QueryParser.AllowLeadingWildcard"/>
        /// and <see cref="QueryParsers.QueryParser.Field"/> can be customized to the clients needs.
        /// </summary>
        /// <typeparam name="T">The type of document that queries will be built against.</typeparam>
        public FieldMappingQueryParser<T> CreateQueryParser<T>()
        {
            var mapper = new ReflectionDocumentMapper<T>(version, externalAnalyzer);
            return new FieldMappingQueryParser<T>(version, mapper) { DefaultSearchProperty = mapper.KeyProperties.FirstOrDefault() ?? mapper.IndexedProperties.FirstOrDefault()};
        }

        /// <summary>
        /// Create a <see cref="QueryParsers.QueryParser"/> suitable for parsing advanced queries
        /// that cannot not expressed as LINQ (e.g. queries submitted by a user).
        ///
        /// After the instance is returned, options such as <see cref="QueryParsers.QueryParser.AllowLeadingWildcard"/>
        /// and <see cref="QueryParsers.QueryParser.Field"/> can be customized to the clients needs.
        /// </summary>
        /// <typeparam name="T">The type of document that queries will be built against.</typeparam>
        /// <param name="defaultSearchField">The default field for queries that don't specify which field to search.
        /// For an example query like <c>Lucene OR NuGet</c>, if this argument is set to <c>SearchText</c>,
        /// it will produce a query like <c>SearchText:Lucene OR SearchText:NuGet</c>.</param>
        /// <returns></returns>
        public FieldMappingQueryParser<T> CreateQueryParser<T>(string defaultSearchField)
        {
            var mapper = new ReflectionDocumentMapper<T>(version, externalAnalyzer);
            return new FieldMappingQueryParser<T>(version, defaultSearchField, mapper);
        }

        /// <summary>
        /// Gets the index format version provided by constructor.
        /// </summary>
        public Version LuceneVersion
        {
            get { return version; }
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>() where T : new()
        {
            return AsQueryable(() => new T());
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(IDocumentMapper<T> documentMapper) where T : new()
        {
            return AsQueryable(() => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(ObjectFactory<T> factory)
        {
            return AsQueryable(_ => factory());
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(ObjectLookup<T> lookup)
        {
            return AsQueryable(lookup, new ReflectionDocumentMapper<T>(version, externalAnalyzer));
        }

        /// <summary>
        /// <see cref="AsQueryable{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public IQueryable<T> AsQueryable<T>(ObjectFactory<T> factory, IDocumentMapper<T> documentMapper)
        {
            return AsQueryable(_ => factory(), documentMapper);
        }

        /// <summary>
        /// Returns an IQueryable implementation where the type being mapped
        /// from <c cref="Document"/> is constructed by a factory delegate.
        /// </summary>
        /// <typeparam name="T">The type of object that Document will be mapped onto.</typeparam>
        /// <param name="lookup">Factory method to instantiate new instances of T.</param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        public IQueryable<T> AsQueryable<T>(ObjectLookup<T> lookup, IDocumentMapper<T> documentMapper)
        {
            return CreateQueryable(lookup, context, documentMapper);
        }

        /// <summary>
        /// Returns an enumeration of fields names that are indexed for a given object.
        /// This may be useful in conjunction with <see cref="CreateQueryParser{T}"/> to
        /// allow users to specify advanced custom queries.
        /// </summary>
        public IEnumerable<string> GetIndexedPropertyNames<T>()
        {
            return new ReflectionDocumentMapper<T>(version, externalAnalyzer).IndexedProperties;
        }

        /// <summary>
        /// <see cref="OpenSession{T}(ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>() where T : new()
        {
            return OpenSession(() => new T());
        }

        /// <summary>
        /// <see cref="OpenSession{T}(ObjectFactory{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(IDocumentMapper<T> documentMapper) where T : new()
        {
            return OpenSession(() => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="OpenSession{T}(ObjectFactory{T}, IDocumentMapper{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(ObjectFactory<T> factory)
        {
            return OpenSession(_ => factory());
        }

        /// <summary>
        /// <see cref="OpenSession{T}(ObjectLookup{T}, IDocumentMapper{T}, IDocumentModificationDetector{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(ObjectLookup<T> lookup)
        {
            var reflectionDocumentMapper = new ReflectionDocumentMapper<T>(version, externalAnalyzer);

            return OpenSession(lookup, reflectionDocumentMapper, reflectionDocumentMapper);
        }

        /// <summary>
        /// <see cref="OpenSession{T}(ObjectLookup{T}, IDocumentMapper{T}, IDocumentModificationDetector{T})"/>
        /// </summary>
        public ISession<T> OpenSession<T>(ObjectFactory<T> factory, IDocumentMapper<T> documentMapper)
        {
            return OpenSession(_ => factory(), documentMapper);
        }

        /// <summary>
        /// Opens a session for staging changes and then committing them atomically.
        /// </summary>
        /// <param name="lookup">Factory delegate that creates new instances of <typeparamref name="T"/></param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        /// <typeparam name="T">The type of object that will be mapped to <c cref="Document"/>.</typeparam>
        public ISession<T> OpenSession<T>(ObjectLookup<T> lookup, IDocumentMapper<T> documentMapper)
        {
            var documentModificationDetector = documentMapper as IDocumentModificationDetector<T>;

            if (documentModificationDetector == null)
            {
                throw new ArgumentException(
                    string.Format("The type {0} must implement {1} or else a separate implementation of {1} must be provided using an alternate overload.",
                                  documentMapper.GetType(),
                                  typeof(IDocumentModificationDetector<T>)),
                    "documentMapper");
            }

            return OpenSession(lookup, documentMapper, documentModificationDetector);
        }

        /// <summary>
        /// Opens a session for staging changes and then committing them atomically.
        /// </summary>
        /// <param name="factory">Factory delegate that creates new instances of <typeparamref name="T"/></param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        /// <param name="documentModificationDetector">Helper to determine when instances of <typeparamref name="T"/> are modified
        ///     and need to be updated in the index when the session is committed.
        /// </param>
        /// <typeparam name="T">The type of object that will be mapped to <c cref="Document"/>.</typeparam>
        public ISession<T> OpenSession<T>(ObjectFactory<T> factory, IDocumentMapper<T> documentMapper, IDocumentModificationDetector<T> documentModificationDetector)
        {
            return OpenSession(_ => factory(), documentMapper, documentModificationDetector);
        }

        /// <summary>
        /// Opens a session for staging changes and then committing them atomically.
        /// </summary>
        /// <param name="lookup">Factory delegate that resolves instances of <typeparamref name="T"/></param>
        /// <param name="documentMapper">Mapper that will convert documents to objects and vice versa.</param>
        /// <param name="documentModificationDetector">Helper to determine when instances of <typeparamref name="T"/> are modified
        ///     and need to be updated in the index when the session is committed.
        /// </param>
        /// <typeparam name="T">The type of object that will be mapped to <c cref="Document"/>.</typeparam>
        public virtual ISession<T> OpenSession<T>(ObjectLookup<T> lookup, IDocumentMapper<T> documentMapper, IDocumentModificationDetector<T> documentModificationDetector)
        {
            perFieldAnalyzer.Merge(documentMapper.Analyzer);

            return new LuceneSession<T>(
                documentMapper,
                documentModificationDetector,
                IndexWriter,
                context,
                CreateQueryable(lookup, context, documentMapper));
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback) where T : new()
        {
            RegisterCacheWarmingCallback(callback, _ => new T());
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, IDocumentMapper<T> documentMapper) where T : new()
        {
            RegisterCacheWarmingCallback(callback, _ => new T(), documentMapper);
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(Action{System.Linq.IQueryable{T}}, ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, ObjectFactory<T> factory)
        {
            RegisterCacheWarmingCallback(callback, _ => factory());
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(System.Action{System.Linq.IQueryable{T}}, ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, ObjectFactory<T> factory, IDocumentMapper<T> documentMapper)
        {
            RegisterCacheWarmingCallback(callback, _ => factory(), documentMapper);
        }

        /// <summary>
        /// <see cref="RegisterCacheWarmingCallback{T}(System.Action{System.Linq.IQueryable{T}}, ObjectLookup{T}, IDocumentMapper{T})"/>
        /// </summary>
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, ObjectLookup<T> lookup)
        {
            RegisterCacheWarmingCallback(callback, lookup, new ReflectionDocumentMapper<T>(version, null));
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
        public void RegisterCacheWarmingCallback<T>(Action<IQueryable<T>> callback, ObjectLookup<T> lookup, IDocumentMapper<T> documentMapper)
        {
            context.SearcherLoading += (s, e) =>
            {
                Log.Trace(m => m("Invoking cache warming callback " + lookup));

                var warmupContext = new WarmUpContext(context, e.IndexSearcher);
                var queryable = CreateQueryable(lookup, warmupContext, documentMapper);
                callback(queryable);

                Log.Trace(m => m("Callback {0} completed.", lookup));
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
            context.Dispose();

            if (writerIsExternal) return;

            if (writer != null)
            {
                writer.Dispose();
            }
        }

        protected virtual IIndexWriter GetIndexWriter(Analyzer analyzer)
        {
            var indexWriter = new IndexWriter(directory, analyzer, ShouldCreateIndex, DeletionPolicy, MaxFieldLength)
            {
                MergeFactor = Settings.MergeFactor
            };
            indexWriter.SetRAMBufferSizeMB(Settings.RAMBufferSizeMB);
            if (Settings.MergePolicyBuilder != null)
            {
                indexWriter.SetMergePolicy(Settings.MergePolicyBuilder(indexWriter));
            }
            return new IndexWriterAdapter(indexWriter);
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
            get { return Settings.DeletionPolicy; }
        }

        protected virtual IndexWriter.MaxFieldLength MaxFieldLength
        {
            get { return Settings.MaxFieldLength; }
        }

        private LuceneQueryable<T> CreateQueryable<T>(ObjectLookup<T> factory, Context context, IDocumentMapper<T> mapper)
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
