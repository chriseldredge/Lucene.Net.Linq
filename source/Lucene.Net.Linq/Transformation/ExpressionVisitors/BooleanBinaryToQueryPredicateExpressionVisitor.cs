using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces boolean binary expressions like <c>[LuceneQueryPredicateExpression](+field:query) == false</c> to <c>[LuceneQueryPredicateExpression](-field:query)</c>
    /// </summary>
    internal class BooleanBinaryToQueryPredicateExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression expression)
        {
            var predicate = expression.Left as LuceneQueryPredicateExpression;

            var constant = expression.Right.IsTrueConstant();

            if (predicate == null || !(constant || expression.Right.IsFalseConstant()))
            {
                return base.VisitBinary(expression);
            }

            if ((expression.NodeType == ExpressionType.Equal && constant) ||
                (expression.NodeType == ExpressionType.NotEqual && !constant))
            {
                return predicate;
            }

            return new LuceneQueryPredicateExpression(predicate.QueryField, predicate.QueryPattern, Occur.MUST_NOT, predicate.QueryType)
                    { Boost = predicate.Boost, AllowSpecialCharacters = predicate.AllowSpecialCharacters };
        }
    }
}
