using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal class BoostMethodCallVisitor : MethodInfoMatchingVisitor
    {
        private readonly int stage;
        private static readonly MethodInfo BoostMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Boost<object>(null, 0f));

        internal BoostMethodCallVisitor(int stage)
        {
            this.stage = stage;

            AddMethod(BoostMethod);
        }

        protected override Expression VisitSupportedMethodCall(MethodCallExpression expression)
        {
            if (stage == 0)
            {
                return VisitAsField(expression);
            }

            return VisitAsBinaryExpression(expression);

        }

        private Expression VisitAsField(MethodCallExpression expression)
        {
            var queryField = expression.Arguments[0] as LuceneQueryFieldExpression;

            if (queryField == null)
            {
                return expression;
            }

            queryField.Boost = (float)((ConstantExpression)expression.Arguments[1]).Value;

            return queryField;
        }

        private Expression VisitAsBinaryExpression(MethodCallExpression expression)
        {
            var query = expression.Arguments[0] as LuceneQueryPredicateExpression;

            if (query != null)
            {
                query.Boost = GetBoost(expression);
                return query;
            }

            var binary = expression.Arguments[0] as BinaryExpression;

            if (binary != null)
            {
                return new BoostBinaryExpression(binary, GetBoost(expression));
            }

            throw new NotSupportedException("Boost() may only be applied on expressions appearing within a where clause.");
        }

        private static float GetBoost(MethodCallExpression expression)
        {
            return (float)((ConstantExpression)expression.Arguments[1]).Value;
        }
    }
}
