using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using Remotion.Linq.Parsing.Structure;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    public class LuceneDataProvider
    {
        public static readonly Version DefaultVersion = Version.LUCENE_29;

        private readonly object sync = new object();
        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private readonly IQueryParser queryParser;
        private IndexWriter indexWriter;

        public LuceneDataProvider(Directory directory)
            : this(directory, new StandardAnalyzer(DefaultVersion), DefaultVersion)
        {
        }

        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version)
            : this(directory, analyzer, version, null)
        {
        }

        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version, IndexWriter indexWriter)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;
            this.indexWriter = indexWriter;

            queryParser = QueryParser.CreateDefault();
        }

        public void AddDocument(object item)
        {
            var mapper = new ReflectionDocumentMapper<object>(item.GetType());
            var document = new Document();
            mapper.ToDocument(item, document);

            IndexWriter.AddDocument(document);
            IndexWriter.Commit();
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

        internal IndexWriter IndexWriter
        {
            get
            {
                lock(sync)
                {
                    if (indexWriter == null)
                    {
                        indexWriter = new IndexWriter(directory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
                    }
                    return indexWriter;
                }
            }
        }
    }
}