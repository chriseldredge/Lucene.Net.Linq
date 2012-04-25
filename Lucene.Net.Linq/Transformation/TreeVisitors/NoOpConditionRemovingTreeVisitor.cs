using System.Linq.Expressions;
using Lucene.Net.Linq.Util;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Converts pointless BinaryExpressions like "True AndAlso Expression"
    /// or "False OrElse Expression" to take only the right side.  Applies
    /// recursively to collapse deeply nested pointless expressions.
    /// </summary>
    internal class NoOpConditionRemovingTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var result = base.VisitBinaryExpression(expression);

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