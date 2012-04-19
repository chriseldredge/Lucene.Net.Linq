using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces supported method calls like [LuceneQueryFieldExpression].StartsWith("foo") with a BinaryExpression like [LuceneQueryFieldExpression] == foo*
    /// </summary>
    internal class MethodCallToBinaryExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            var queryField = expression.Object as LuceneQueryFieldExpression;

            if (queryField == null)
                return base.VisitMethodCallExpression(expression);

            if (expression.Method.Name == "StartsWith")
            {
                return new LuceneQueryExpression(queryField, expression.Arguments[0], BooleanClause.Occur.MUST, QueryType.Prefix);
            }
            
            return base.VisitMethodCallExpression(expression);
        }
    }
}