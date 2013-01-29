using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Lucene.Net.Linq.Clauses.ExpressionNodes
{
    internal class BoostExpressionNode : MethodCallExpressionNodeBase
    {
        public static readonly MethodInfo[] SupportedMethods = new[]
            {
                GetSupportedMethod (() => LuceneMethods.BoostInternal<object> (null, null))
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

        protected override QueryModel ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            queryModel.BodyClauses.Add(new BoostClause(boostFunction));
            return queryModel;
        }
    }
}