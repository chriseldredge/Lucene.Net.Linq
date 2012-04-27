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
        private static readonly MethodInfo AnyFieldMethod;
        private static readonly MethodInfo ScoreMethod;

        static LuceneExtensionMethodCallTreeVisitor()
        {
            AnyFieldMethod = typeof(LuceneMethods).GetMethods().Where(m => m.Name == "AnyField").Single();
            ScoreMethod = typeof (LuceneMethods).GetMethods().Where(m => m.Name == "Score").Single();

            if (AnyFieldMethod == null || ScoreMethod == null)
            {
                throw new InvalidOperationException("Failed to load MethodInfo for extension methods on LuceneMethods.");
            }
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (MethodsEqual(expression.Method, AnyFieldMethod))
            {
                return LuceneQueryAnyFieldExpression.Instance;
            }

            if (MethodsEqual(expression.Method, ScoreMethod))
            {
                return LuceneOrderByRelevanceExpression.Instance;
            }

            return base.VisitMethodCallExpression(expression);
        }

        private bool MethodsEqual(MethodInfo methodInfo, MethodInfo baseMethod)
        {
            return methodInfo.IsGenericMethod && methodInfo == baseMethod.MakeGenericMethod(methodInfo.GetGenericArguments());
        }
    }
}