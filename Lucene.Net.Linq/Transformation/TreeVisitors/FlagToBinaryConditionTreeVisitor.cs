using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class FlagToBinaryConditionTreeVisitor : ExpressionTreeVisitor
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

            negate = true;

            var result = VisitExpression(expression.Operand);

            negate = false;

            return result;
        }

        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            var field = expression as LuceneQueryFieldExpression;

            if (field == null || field.Type != typeof(bool) || IsAlreadyInEqualityExpression())
            {
                return base.VisitExtensionExpression(expression);    
            }

            return Expression.MakeBinary(ExpressionType.Equal, field, Expression.Constant(!negate));
        }

        private bool IsAlreadyInEqualityExpression()
        {
            if (!(parent is BinaryExpression)) return false;

            var binary = (BinaryExpression) parent;

            return binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual;
        }
    }
}