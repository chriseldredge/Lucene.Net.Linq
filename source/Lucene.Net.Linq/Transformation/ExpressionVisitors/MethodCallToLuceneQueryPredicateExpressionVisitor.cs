using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces supported method calls like [LuceneQueryFieldExpression].StartsWith("foo") with a LuceneQueryPredicateExpression like [LuceneQueryPredicateExpression](+Field:foo*)
    /// </summary>
    internal class MethodCallToLuceneQueryPredicateExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var queryField = expression.Object as LuceneQueryFieldExpression;

            if (queryField == null)
                return base.VisitMethodCall(expression);

            if (expression.Method.Name == "StartsWith")
            {
                return new LuceneQueryPredicateExpression(queryField, expression.Arguments[0], Occur.MUST, QueryType.Prefix);
            }
            if (expression.Method.Name == "EndsWith")
            {
                return new LuceneQueryPredicateExpression(queryField, expression.Arguments[0], Occur.MUST, QueryType.Suffix);
            }
            if (expression.Method.Name == "Contains")
            {
                return new LuceneQueryPredicateExpression(queryField, expression.Arguments[0], Occur.MUST, QueryType.Wildcard);
            }
            
            return base.VisitMethodCall(expression);
        }
    }
}