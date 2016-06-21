using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal class AllowSpecialCharactersMethodExpressionVisitor : RelinqExpressionVisitor
    {
        private bool allowed;
        private LuceneQueryPredicateExpression parent;

        protected override Expression VisitExtension(Expression expression)
        {
            if (expression is AllowSpecialCharactersExpression)
            {
                return VisitAllowSpecialCharactersExpression((AllowSpecialCharactersExpression) expression);
            }

            if (expression is LuceneQueryPredicateExpression)
            {
                return VisitQueryPredicateExpression((LuceneQueryPredicateExpression) expression);
            }

            return base.VisitExtension(expression);
        }

        private Expression VisitAllowSpecialCharactersExpression(AllowSpecialCharactersExpression expression)
        {
            allowed = true;

            if (parent != null)
            {
                parent.AllowSpecialCharacters = true;
            }

            var result = Visit(expression.Pattern);

            allowed = false;

            return result;
        }

        private Expression VisitQueryPredicateExpression(LuceneQueryPredicateExpression expression)
        {
            parent = expression;

            var result = base.VisitExtension(expression);

            if (allowed && result is LuceneQueryPredicateExpression)
            {
                ((LuceneQueryPredicateExpression)result).AllowSpecialCharacters = true;
            }

            parent = null;

            return result;
        }
    }
}
