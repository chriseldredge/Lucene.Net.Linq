using Lucene.Net.Linq.Clauses.ExpressionNodes;
using Lucene.Net.Linq.Transformation;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.ExpressionTreeProcessors;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

namespace Lucene.Net.Linq
{
    internal static class RelinqQueryParserFactory
    {
        internal static QueryParser CreateQueryParser()
        {
            var expressionTreeParser = new ExpressionTreeParser(
                CreateNodeTypeProvider(),
                CreateExpressionTreeProcessor());

            return new QueryParser(expressionTreeParser);
        }

        private static INodeTypeProvider CreateNodeTypeProvider()
        {
            var registry = new MethodInfoBasedNodeTypeRegistry();
            registry.Register(BoostExpressionNode.SupportedMethods, typeof(BoostExpressionNode));
            registry.Register(QueryStatisticsCallbackExpressionNode.SupportedMethods, typeof(QueryStatisticsCallbackExpressionNode));
            registry.Register(TrackRetrievedDocumentsExpressionNode.SupportedMethods, typeof(TrackRetrievedDocumentsExpressionNode));

            var nodeTypeProvider = ExpressionTreeParser.CreateDefaultNodeTypeProvider();
            nodeTypeProvider.InnerProviders.Add(registry);

            return nodeTypeProvider;
        }

        /// <summary>
        /// Creates an <c cref="IExpressionTreeProcessor"/> that will execute
        /// <c cref="AllowSpecialCharactersExpressionTransformer"/>
        /// before executing <c cref="PartialEvaluatingExpressionTreeProcessor"/>
        /// and other default processors.
        /// </summary>
        internal static IExpressionTreeProcessor CreateExpressionTreeProcessor()
        {
            var firstRegistry = new ExpressionTransformerRegistry();
            firstRegistry.Register(new AllowSpecialCharactersExpressionTransformer());

            var processor = ExpressionTreeParser.CreateDefaultProcessor(ExpressionTransformerRegistry.CreateDefault());
            processor.InnerProcessors.Insert(0, new TransformingExpressionTreeProcessor(firstRegistry));
            return processor;
        }
    }
}
