using System;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Util
{
    internal static class ExpressionExtensions
    {
        internal static bool IsZeroConstant(this Expression expression)
        {
            return (expression is ConstantExpression) && Convert.ToInt32(((ConstantExpression)expression).Value) == 0;
        }

        internal static bool IsNullConstant(this Expression expression)
        {
            var constant = expression as ConstantExpression;

            if (constant == null) return false;

            return (constant.Value == null);
        }

        internal static bool IsTrueConstant(this Expression expression)
        {
            return expression.Type == typeof(bool) && expression is ConstantExpression && Convert.ToBoolean(((ConstantExpression)expression).Value);
        }

        internal static bool IsFalseConstant(this Expression expression)
        {
            return expression.Type == typeof(bool) && expression is ConstantExpression && !Convert.ToBoolean(((ConstantExpression)expression).Value);
        }
    }
}