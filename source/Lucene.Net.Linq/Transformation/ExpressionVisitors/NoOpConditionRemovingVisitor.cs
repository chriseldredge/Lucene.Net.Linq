using System.Linq.Expressions;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Converts pointless BinaryExpressions like "True AndAlso Expression"
    /// or "False OrElse Expression" to take only the right side.  Applies
    /// recursively to collapse deeply nested pointless expressions.
    /// </summary>
    internal class NoOpConditionRemovingVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression expression)
        {
            var result = base.VisitBinary(expression);

            if (result is BinaryExpression)
            {
                expression = (BinaryExpression) result;
            }
            else
            {
                return result;
            }

            if (expression.NodeType == ExpressionType.AndAlso && expression.Left.IsTrueConstant())
            {
                return expression.Right;
            }

            if (expression.NodeType == ExpressionType.OrElse && expression.Left.IsFalseConstant())
            {
                return expression.Right;
            }

            return result;
        }
    }
}
