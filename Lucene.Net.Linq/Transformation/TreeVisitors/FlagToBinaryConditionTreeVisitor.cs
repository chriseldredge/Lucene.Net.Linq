using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Clauses.TreeVisitors;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class FlagToBinaryConditionTreeVisitor : LuceneExpressionTreeVisitor
    {
        private Expression parent;
        private bool negate;

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var oldParent = parent;

            parent = expression;

            try
            {
                return base.VisitBinaryExpression(expression);
            }
            finally
            {
                parent = oldParent;
            }
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            if (expression.NodeType != ExpressionType.Not || expression.Type != typeof(bool))
            {
                return base.VisitUnaryExpression(expression);
            }

            negate = !negate;

            var operand = VisitExpression(expression.Operand);

            negate = !negate;

            if (Equals(operand, expression.Operand))
            {
                return Expression.MakeBinary(ExpressionType.Equal, operand, Expression.Constant(negate));
            }

            return operand;
        }

        protected override Expression VisitLuceneQueryFieldExpression(LuceneQueryFieldExpression expression)
        {
            if (expression.Type != typeof(bool) || IsAlreadyInEqualityExpression())
            {
                return base.VisitLuceneQueryFieldExpression(expression);
            }

            return Expression.MakeBinary(ExpressionType.Equal, expression, Expression.Constant(!negate));
        }

        private bool IsAlreadyInEqualityExpression()
        {
            if (!(parent is BinaryExpression)) return false;

            var binary = (BinaryExpression)parent;

            return binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual;
        }
    }
}