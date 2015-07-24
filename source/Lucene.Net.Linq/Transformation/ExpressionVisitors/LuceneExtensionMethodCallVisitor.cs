using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal class LuceneExtensionMethodCallVisitor : MethodInfoMatchingVisitor
    {
        private static readonly MethodInfo AnyFieldMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.AnyField<object>(null));
        private static readonly MethodInfo ScoreMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Score<object>(null));

        public LuceneExtensionMethodCallVisitor()
        {
            AddMethod(AnyFieldMethod);
            AddMethod(ScoreMethod);
        }

        protected override Expression VisitSupportedMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == AnyFieldMethod.Name)
            {
                return LuceneQueryAnyFieldExpression.Instance;
            }

            return LuceneOrderByRelevanceExpression.Instance;
        }
    }
}
