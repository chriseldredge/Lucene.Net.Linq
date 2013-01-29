using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces supported method calls like [LuceneQueryFieldExpression].StartsWith("foo") with a LuceneQueryPredicateExpression like [LuceneQueryPredicateExpression](+Field:foo*)
    /// </summary>
    internal class MethodCallToLuceneQueryPredicateExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            var queryField = expression.Object as LuceneQueryFieldExpression;

            if (queryField == null)
                return base.VisitMethodCallExpression(expression);

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
            
            return base.VisitMethodCallExpression(expression);
        }
    }
}