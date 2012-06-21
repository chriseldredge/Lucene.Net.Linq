using Lucene.Net.Linq.Transformation;
using Remotion.Linq.Parsing.ExpressionTreeVisitors.Transformation;
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

        private static CompoundNodeTypeProvider CreateNodeTypeProvider()
        {
            var registry = MethodInfoBasedNodeTypeRegistry.CreateFromTypes(typeof (RelinqQueryParserFactory).Assembly.GetTypes());
            
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