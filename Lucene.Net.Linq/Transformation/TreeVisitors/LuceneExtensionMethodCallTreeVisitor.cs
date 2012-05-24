using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class LuceneExtensionMethodCallTreeVisitor : ExpressionTreeVisitor
    {
        private static readonly MethodInfo AnyFieldMethod;
        private static readonly MethodInfo ScoreMethod;

        static LuceneExtensionMethodCallTreeVisitor()
        {
            AnyFieldMethod = ReflectionUtility.GetMethod(() => LuceneMethods.AnyField<object>(null)).GetGenericMethodDefinition();
            ScoreMethod = ReflectionUtility.GetMethod(() => LuceneMethods.Score<object>(null)).GetGenericMethodDefinition();
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

        internal static bool MethodsEqual(MethodInfo methodInfo, MethodInfo baseMethod)
        {
            return methodInfo.IsGenericMethod && methodInfo == baseMethod.MakeGenericMethod(methodInfo.GetGenericArguments());
        }
    }
}