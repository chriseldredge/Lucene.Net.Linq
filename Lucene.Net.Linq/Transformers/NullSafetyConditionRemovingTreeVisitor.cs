using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Util;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformers
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
            if (!(expression.Test is BinaryExpression)) return expression;

            var test = (BinaryExpression)expression.Test;

            var testExpression = GetNonNullSide(test.Left, test.Right);
            var nonNullResult = GetNonNullSide(expression.IfFalse, expression.IfTrue);

            if (testExpression == null || nonNullResult == null) return expression;

            if (ReflectionUtils.ReflectionEquals(testExpression, nonNullResult))
            {
                return nonNullResult;
            }

            return expression;
        }

        private static Expression GetNonNullSide(Expression a, Expression b)
        {
            if (IsNullConstant(a)) return b;
            if (IsNullConstant(b)) return a;

            return null;
        }

        private static bool IsNullConstant(Expression expression)
        {
            return expression is ConstantExpression && ((ConstantExpression) expression).Value == null;
        }
    }
}