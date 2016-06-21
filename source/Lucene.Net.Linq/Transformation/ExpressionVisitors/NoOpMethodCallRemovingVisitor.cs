using System.Collections.Generic;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Removes method calls like string.ToLower() that have no effect on a query due to
    /// case sensitivity in Lucene being configured elsewhere by the Analyzer.
    /// </summary>
    internal class NoOpMethodCallRemovingVisitor : ExpressionVisitor
    {
        private static readonly ISet<string> NoOpMethods =
            new HashSet<string>
                {
                    "ToLower",
                    "ToLowerInvariant",
                    "ToUpper",
                    "ToUpeprInvariant"
                };

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (NoOpMethods.Contains(expression.Method.Name))
            {
                return expression.Object;
            }

            return base.VisitMethodCall(expression);
        }
    }
}
