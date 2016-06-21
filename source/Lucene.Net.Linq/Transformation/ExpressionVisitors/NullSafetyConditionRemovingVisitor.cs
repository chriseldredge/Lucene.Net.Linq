using System.Linq.Expressions;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Locates expressions like IFF(x != null, x, null) and converts them to x.
    /// When combined with <c ref="NoOpMethodCallRemovingTreeVisitor"/> a null-safe
    /// ToLower operation like IFF(x != null, x.ToLower(), null) is simplified to x.
    /// </summary>
    internal class NullSafetyConditionRemovingVisitor : ExpressionVisitor
    {
        protected override Expression VisitConditional(ConditionalExpression expression)
        {
            var result = base.VisitConditional(expression);

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
            if (a.IsNullConstant() || a.IsFalseConstant()) return b;
            if (b.IsNullConstant() || b.IsFalseConstant()) return a;

            return null;
        }
    }
}
