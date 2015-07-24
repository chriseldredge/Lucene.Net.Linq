using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Util;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Lucene.Net.Linq.Clauses.ExpressionNodes
{
    internal class TrackRetrievedDocumentsExpressionNode : MethodCallExpressionNodeBase
    {
        public static readonly MethodInfo[] SupportedMethods =
            {
                MemberInfoUtils.GetGenericMethod(() => LuceneMethods.TrackRetrievedDocuments<object>(null, null))
            };

        private readonly ConstantExpression tracker;

        public TrackRetrievedDocumentsExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression tracker)
            : base(parseInfo)
        {
            this.tracker = tracker;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        protected override QueryModel ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            queryModel.BodyClauses.Add(new TrackRetrievedDocumentsClause(tracker));
            return queryModel;
        }
    }
}
