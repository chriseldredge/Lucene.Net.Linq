using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal class FuzzyMethodCallVisitor : MethodInfoMatchingVisitor
    {
        private static readonly MethodInfo FuzzyMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Fuzzy(false, 0f));

        internal FuzzyMethodCallVisitor()
        {
            AddMethod(FuzzyMethod);
        }

        protected override Expression VisitSupportedMethodCall(MethodCallExpression expression)
        {
            var query = expression.Arguments[0] as LuceneQueryPredicateExpression;

            if (query == null)
                throw new NotSupportedException("Fuzzy is only supported after predicate expressions. Example: (x.Field == \"term\").Fuzzy(0.6f)");

            if (query.QueryType != QueryType.Default)
                throw new NotSupportedException("Fuzzy in only supported with default queries. Example: (x.Field == \"term\")");

            query.Fuzzy = GetFuzzy(expression);

            return query;
        }

        private static float GetFuzzy(MethodCallExpression expression)
        {
            return (float)((ConstantExpression)expression.Arguments[1]).Value;
        }
    }
}
