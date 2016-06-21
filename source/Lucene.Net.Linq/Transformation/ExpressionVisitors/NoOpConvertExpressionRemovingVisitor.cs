using System;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces expressions like <c>(bool)(Constant(bool?))</c> with <c>Constant(bool?)</c>.
    /// </summary>
    internal class NoOpConvertExpressionRemovingVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression expression)
        {
            var left = base.Visit(expression.Left);
            var right = base.Visit(expression.Right);

            if (ReferenceEquals(left, expression.Left) && ReferenceEquals(right, expression.Right))
            {
                return expression;
            }

            left = ConvertIfNecessary(left, right.Type);
            right = ConvertIfNecessary(right, left.Type);

            return Expression.MakeBinary(expression.NodeType, left, right);
        }

        private Expression ConvertIfNecessary(Expression expression, Type type)
        {
            var constant = expression as ConstantExpression;
            if (constant == null || expression.Type == type) return expression;

            if (type.IsEnum)
            {
                return Expression.Constant(Enum.ToObject(type, constant.Value));
            }

            return Expression.Constant(Convert.ChangeType(constant.Value, type));
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                return base.Visit(expression.Operand);
            }

            return expression.Update(Visit(expression.Operand));
        }
    }
}
