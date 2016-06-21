using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Util;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;

namespace Lucene.Net.Linq.Transformation
{
    internal class AllowSpecialCharactersExpressionTransformer : IExpressionTransformer<MethodCallExpression>
    {
        public Expression Transform(MethodCallExpression expression)
        {
            if (expression.Method.Name != MemberInfoUtils.GetGenericMethod(() => LuceneMethods.AllowSpecialCharacters<object>(null)).Name)
            {
                return expression;
            }

            return new AllowSpecialCharactersExpression(expression.Arguments[0]);
        }

        public ExpressionType[] SupportedExpressionTypes
        {
            get { return new[] {ExpressionType.Call}; }
        }
    }
}
