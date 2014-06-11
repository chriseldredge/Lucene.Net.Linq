using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class FuzzyMethodCallTreeVisitor : MethodInfoMatchingTreeVisitor
    {
        private static readonly MethodInfo FuzzyMethod = ReflectionUtility.GetMethod(() => LuceneMethods.Fuzzy(false, 0f));

        internal FuzzyMethodCallTreeVisitor()
        {
            AddMethod(FuzzyMethod);
        }

        protected override Expression VisitSupportedMethodCallExpression(MethodCallExpression expression)
        {
            var query = expression.Arguments[0] as LuceneQueryPredicateExpression;

            if (query != null)
            {
                query.Fuzzy = GetFuzzy(expression);
                return query;
            }

            throw new NotSupportedException();
        }

        private static float GetFuzzy(MethodCallExpression expression)
        {
            return (float)((ConstantExpression)expression.Arguments[1]).Value;
        }
    }
}
