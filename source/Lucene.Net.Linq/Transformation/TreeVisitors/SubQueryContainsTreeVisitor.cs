using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces subqueries like {[doc].Tags => Contains("c")} with BinaryExpressions like ([doc].Tags == "c").
    /// </summary>
    internal class SubQueryContainsTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            var operators = expression.QueryModel.ResultOperators;

            if (operators.Count == 1 && operators[0] is ContainsResultOperator)
            {
                var op = (ContainsResultOperator) operators[0];

                var field = expression.QueryModel.MainFromClause.FromExpression;
                var pattern = op.Item;
                if (pattern.Type.IsPrimitive)
                {
                    pattern = Expression.Constant(((ConstantExpression)pattern).Value, typeof(object));
                }

                return Expression.MakeBinary(ExpressionType.Equal, field, pattern);
            }

            return base.VisitSubQueryExpression(expression);
        }
    }
}
