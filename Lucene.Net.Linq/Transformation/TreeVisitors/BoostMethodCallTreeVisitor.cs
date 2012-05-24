using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class BoostMethodCallTreeVisitor : ExpressionTreeVisitor
    {
        private readonly int stage;
        private static readonly MethodInfo BoostMethod;

        static BoostMethodCallTreeVisitor()
        {
            BoostMethod = ReflectionUtility.GetMethod(() => LuceneMethods.Boost<object>(null, 0f)).GetGenericMethodDefinition();
        }

        internal BoostMethodCallTreeVisitor(int stage)
        {
            this.stage = stage;
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (!LuceneExtensionMethodCallTreeVisitor.MethodsEqual(expression.Method, BoostMethod))
            {
                return base.VisitMethodCallExpression(expression);
            }

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
                //throw new NotSupportedException("Boost() may only be applied on query fields appearing within a where clause.");
            }

            queryField.Boost = (float)((ConstantExpression)expression.Arguments[1]).Value;

            return queryField;
        }

        private Expression VisitAsBinaryExpression(MethodCallExpression expression)
        {
            var query = expression.Arguments[0] as LuceneQueryExpression;

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