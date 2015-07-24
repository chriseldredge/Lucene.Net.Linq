using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Util;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Lucene.Net.Linq.Clauses.ExpressionNodes
{
    internal class QueryStatisticsCallbackExpressionNode : MethodCallExpressionNodeBase
    {
        public static readonly MethodInfo[] SupportedMethods =
            {
                MemberInfoUtils.GetGenericMethod(() => LuceneMethods.CaptureStatistics<object>(null, null))
            };

        private readonly ConstantExpression callback;

        public QueryStatisticsCallbackExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression callback)
            : base(parseInfo)
        {
            this.callback = callback;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            queryModel.BodyClauses.Add(new QueryStatisticsCallbackClause(callback));
        }
    }
}
