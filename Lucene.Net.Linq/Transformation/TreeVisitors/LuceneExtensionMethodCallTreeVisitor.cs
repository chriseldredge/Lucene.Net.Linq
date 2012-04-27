using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class LuceneExtensionMethodCallTreeVisitor : ExpressionTreeVisitor
    {
        private static readonly MethodInfo method;

        static LuceneExtensionMethodCallTreeVisitor()
        {
            method = typeof (LuceneMethods).GetMethods().Where(m => m.Name == "Score").Single();

            if (method == null)
            {
                throw new InvalidOperationException("Failed to load MethodInfo for LuceneMethods.Score");
            }
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (IsScoreExtensionMethod(expression.Method))
            {
                return LuceneOrderByRelevanceExpression.Instance;
            }

            return base.VisitMethodCallExpression(expression);
        }

        private bool IsScoreExtensionMethod(MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo == method.MakeGenericMethod(methodInfo.GetGenericArguments());
        }
    }
}