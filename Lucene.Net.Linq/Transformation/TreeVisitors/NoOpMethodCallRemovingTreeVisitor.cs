using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Removes method calls like string.ToLower() that have no effect on a query due to
    /// case sensitivity in Lucene being configured elsewhere by the Analyzer.
    /// </summary>
    internal class NoOpMethodCallRemovingTreeVisitor : ExpressionTreeVisitor
    {
        private static readonly ISet<string> NoOpMethods =
            new HashSet<string>
                {
                    "ToLower",
                    "ToLowerInvariant",
                    "ToUpper",
                    "ToUpeprInvariant"
                };

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (NoOpMethods.Contains(expression.Method.Name))
            {
                return expression.Object;
            }

            return base.VisitMethodCallExpression(expression);
        }
    }
}