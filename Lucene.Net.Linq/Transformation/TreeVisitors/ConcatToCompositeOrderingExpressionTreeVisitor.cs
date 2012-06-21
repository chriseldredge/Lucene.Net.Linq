using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces method calls like string.Concat([LuceneQueryFieldExpression], [LuceneQueryFieldExpression]) to LuceneCompositeOrderingExpression
    /// </summary>
    internal class ConcatToCompositeOrderingExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Concat" && expression.Arguments.Count == 2)
            {
                var fields = new[] { (LuceneQueryFieldExpression)expression.Arguments[0], (LuceneQueryFieldExpression)expression.Arguments[1] };
                return new LuceneCompositeOrderingExpression(fields);
            }
            
            return base.VisitMethodCallExpression(expression);
        }
    }
}