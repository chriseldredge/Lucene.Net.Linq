using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;
using Remotion.Linq;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class LuceneExtensionMethodCallTreeVisitor : MethodInfoMatchingTreeVisitor
    {
        private static readonly MethodInfo AnyFieldMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.AnyField<object>(null));
        private static readonly MethodInfo ScoreMethod = MemberInfoUtils.GetGenericMethod(() => LuceneMethods.Score<object>(null));

        public LuceneExtensionMethodCallTreeVisitor()
        {
            AddMethod(AnyFieldMethod);
            AddMethod(ScoreMethod);
        }

        protected override Expression VisitSupportedMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name == AnyFieldMethod.Name)
            {
                return LuceneQueryAnyFieldExpression.Instance;
            }

            return LuceneOrderByRelevanceExpression.Instance;
        }
    }
}
