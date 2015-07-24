using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces method calls like string.Concat([LuceneQueryFieldExpression], [LuceneQueryFieldExpression]) to LuceneCompositeOrderingExpression
    /// </summary>
    internal class ConcatToCompositeOrderingExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Concat" && expression.Arguments.Count == 2)
            {
                var fields = new[] { (LuceneQueryFieldExpression)expression.Arguments[0], (LuceneQueryFieldExpression)expression.Arguments[1] };
                return new LuceneCompositeOrderingExpression(fields);
            }

            return base.VisitMethodCall(expression);
        }
    }
}
