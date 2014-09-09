using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class AllowSpecialCharactersMethodExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private bool allowed;
        private LuceneQueryPredicateExpression parent;

        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            if (expression is AllowSpecialCharactersExpression)
            {
                return VisitAllowSpecialCharactersExpression((AllowSpecialCharactersExpression) expression);
            }

            if (expression is LuceneQueryPredicateExpression)
            {
                return VisitQueryPredicateExpression((LuceneQueryPredicateExpression) expression);
            }

            return base.VisitExtensionExpression(expression);
        }
        
        private Expression VisitAllowSpecialCharactersExpression(AllowSpecialCharactersExpression expression)
        {
            allowed = true;

            if (parent != null)
            {
                parent.AllowSpecialCharacters = true;
            }

            var result = VisitExpression(expression.Pattern);

            allowed = false;

            return result;
        }

        private Expression VisitQueryPredicateExpression(LuceneQueryPredicateExpression expression)
        {
            parent = expression;

            var result = base.VisitExtensionExpression(expression);

            if (allowed && result is LuceneQueryPredicateExpression)
            {
                ((LuceneQueryPredicateExpression)result).AllowSpecialCharacters = true;
            }

            parent = null;

            return result;
        }
    }
}
