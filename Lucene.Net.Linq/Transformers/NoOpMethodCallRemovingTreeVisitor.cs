using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformers
{
    /// <summary>
    /// Removes method calls like string.ToLower() that have no effect on a query due to
    /// case sensitivity in Lucene being configured elsewhere by the Analyzer.
    /// </summary>
    internal class NoOpMethodCallRemovingTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name == "ToLower")
            {
                return expression.Object;
            }

            return base.VisitMethodCallExpression(expression);
        }
    }
}