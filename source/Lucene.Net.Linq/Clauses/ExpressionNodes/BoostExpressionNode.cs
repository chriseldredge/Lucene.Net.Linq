using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Util;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Lucene.Net.Linq.Clauses.ExpressionNodes
{
    internal class BoostExpressionNode : MethodCallExpressionNodeBase
    {
        public static readonly MethodInfo[] SupportedMethods =
            {
                MemberInfoUtils.GetGenericMethod(() => LuceneMethods.BoostInternal<object>(null, null))
            };

        private readonly LambdaExpression boostFunction;

        public BoostExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression boostFunction) : base(parseInfo)
        {
            this.boostFunction = boostFunction;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            queryModel.BodyClauses.Add(new BoostClause(boostFunction));
        }
    }
}
