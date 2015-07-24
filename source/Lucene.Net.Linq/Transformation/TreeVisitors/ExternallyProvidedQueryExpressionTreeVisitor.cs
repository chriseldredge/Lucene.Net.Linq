using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;
using Remotion.Linq;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces method calls like <c cref="LuceneMethods.Matches{T}">Matches</c> with query expressions.
    /// </summary>
    internal class ExternallyProvidedQueryExpressionTreeVisitor : MethodInfoMatchingTreeVisitor
    {
        private static readonly MethodInfo MatchesMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Matches<object>(null, null));

        internal ExternallyProvidedQueryExpressionTreeVisitor()
        {
            AddMethod(MatchesMethod);
        }

        protected override Expression VisitSupportedMethodCallExpression(MethodCallExpression expression)
        {
            return new LuceneQueryExpression((Query) ((ConstantExpression)expression.Arguments[0]).Value);
        }
    }
}
