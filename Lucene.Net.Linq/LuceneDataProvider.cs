using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Remotion.Linq;
using Remotion.Linq.Parsing.ExpressionTreeVisitors.Transformation;
using Remotion.Linq.Parsing.Structure;

namespace Lucene.Net.Linq
{
    public class LuceneDataProvider
    {
        private readonly IQueryParser queryParser;
        private readonly IQueryExecutor executor;

        public LuceneDataProvider(Directory directory)
        {
            var nodeTypeProvider = ExpressionTreeParser.CreateDefaultNodeTypeProvider();
            //nodeTypeProvider.InnerProviders.Add(customNodeTypeRegistry);
            var transformerRegistry = ExpressionTransformerRegistry.CreateDefault();
            var processor = ExpressionTreeParser.CreateDefaultProcessor(transformerRegistry);
            var expressionTreeParser = new ExpressionTreeParser(nodeTypeProvider, processor);

            queryParser = new QueryParser(expressionTreeParser);
            executor = new LuceneQueryExecutor(directory);
        }

        public IQueryable<Document> AsQueryable()
        {
            return new LuceneQueryable<Document>(queryParser, executor);
        }
    }
}