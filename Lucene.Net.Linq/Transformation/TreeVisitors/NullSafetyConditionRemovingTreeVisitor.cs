using System.Linq.Expressions;
using Lucene.Net.Linq.Util;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Locates expressions like IFF(x != null, x, null) and converts them to x.
    /// When combined with <c ref="NoOpMethodCallRemovingTreeVisitor"/> a null-safe
    /// ToLower operation like IFF(x != null, x.ToLower(), null) is simplified to x.
    /// </summary>
    internal class NullSafetyConditionRemovingTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
            var result = base.VisitConditionalExpression(expression);

            if (!(result is ConditionalExpression)) return result;

            expression = (ConditionalExpression) result;

            if (!(expression.Test is BinaryExpression)) return expression;

            var test = (BinaryExpression)expression.Test;

            var testExpression = GetNonNullSide(test.Left, test.Right);
            var nonNullResult = GetNonNullSide(expression.IfFalse, expression.IfTrue);

            if (testExpression == null || nonNullResult == null) return expression;

            return nonNullResult;
        }

        private static Expression GetNonNullSide(Expression a, Expression b)
        {
            if (a.IsNullConstant()) return b;
            if (b.IsNullConstant()) return a;

            return null;
        }
    }
}