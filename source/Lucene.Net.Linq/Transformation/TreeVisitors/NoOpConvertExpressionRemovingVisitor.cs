using System;
using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces expressions like <c>(bool)(Constant(bool?))</c> with <c>Constant(bool?)</c>.
    /// </summary>
    internal class NoOpConvertExpressionRemovingVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var left = base.VisitExpression(expression.Left);
            var right = base.VisitExpression(expression.Right);

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

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                return base.VisitExpression(expression.Operand);
            }

            return base.VisitUnaryExpression(expression);
        }
    }
}