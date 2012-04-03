using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Remotion.Linq.Parsing.ExpressionTreeVisitors.Transformation;
using Remotion.Linq.Parsing.Structure;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    public class LuceneDataProvider
    {
        public static readonly Version DefaultVersion = new Version("Lucene.Net.Linq", 0);

        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private readonly IQueryParser queryParser;

        public LuceneDataProvider(Directory directory)
            : this(directory, new StandardAnalyzer(DefaultVersion), DefaultVersion)
        {
        }

        public LuceneDataProvider(Directory directory, Analyzer analyzer, Version version)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;

            var nodeTypeProvider = ExpressionTreeParser.CreateDefaultNodeTypeProvider();

            //nodeTypeProvider.InnerProviders.Add();

            var transformerRegistry = ExpressionTransformerRegistry.CreateDefault();

            var processor = ExpressionTreeParser.CreateDefaultProcessor(transformerRegistry);
            var expressionTreeParser = new ExpressionTreeParser(nodeTypeProvider, processor);

            queryParser = new QueryParser(expressionTreeParser);
        }

        public IQueryable<Document> AsQueryable()
        {
            var executor = new DocumentQueryExecutor(directory, new Context(analyzer, version));
            return new LuceneQueryable<Document>(queryParser, executor);
        }

        public IQueryable<T> AsQueryable<T>() where T : IDocumentHolder, new()
        {
            return AsQueryable(() => new T());
        }

        public IQueryable<T> AsQueryable<T>(Func<T> documentFactory) where T : IDocumentHolder
        {
            var executor = new DocumentHolderQueryExecutor<T>(directory, new Context(analyzer, version), documentFactory);
            return new LuceneQueryable<T>(queryParser, executor);
        }
    }
}