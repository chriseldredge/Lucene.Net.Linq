using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces method calls like <c cref="LuceneMethods.Matches{T}">Matches</c> with query expressions.
    /// </summary>
    internal class ExternallyProvidedQueryExpressionVisitor : MethodInfoMatchingVisitor
    {
        private static readonly MethodInfo MatchesMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Matches<object>(null, null));

        internal ExternallyProvidedQueryExpressionVisitor()
        {
            AddMethod(MatchesMethod);
        }

        protected override Expression VisitSupportedMethodCall(MethodCallExpression expression)
        {
            return new LuceneQueryExpression((Query) ((ConstantExpression)expression.Arguments[0]).Value);
        }
    }
}
