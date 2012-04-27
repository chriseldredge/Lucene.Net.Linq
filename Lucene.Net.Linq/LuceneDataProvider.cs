using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using Remotion.Linq.Parsing.Structure;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    public class LuceneDataProvider
    {
        public static readonly Version DefaultVersion = Version.LUCENE_29;

        private readonly object transactionLock;
        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private readonly IQueryParser queryParser;
        private readonly IIndexWriter indexWriter;

        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version, IndexWriter indexWriter)
            : this(directory, analyzer, version, new IndexWriterAdapter(indexWriter), new object())
        {
        }

        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version, IIndexWriter indexWriter, object transactionLock)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;
            this.indexWriter = indexWriter;
            this.transactionLock = transactionLock;

            queryParser = QueryParser.CreateDefault();
        }

        public IQueryable<T> AsQueryable<T>() where T : new()
        {
            return AsQueryable(() => new T());
        }

        public IQueryable<T> AsQueryable<T>(Func<T> factory)
        {
            var executor = new QueryExecutor<T>(directory, new Context(analyzer, version), factory, new ReflectionDocumentMapper<T>());
            return new LuceneQueryable<T>(queryParser, executor);
        }

        public ISession<T> OpenSession<T>()
        {
            var mapper = new ReflectionDocumentMapper<T>();
            return new LuceneSession<T>(mapper, indexWriter, transactionLock);
        }
    }
}