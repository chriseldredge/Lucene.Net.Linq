using System;
using System.Linq.Expressions;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq
{
    public class QueryTransformer
    {
        private readonly Expression<Func<Document>> current;

        public QueryTransformer(Expression<Func<Document>> current)
        {
            this.current = current;
        }

        public Expression Replace(Expression e)
        {
            if (e is MethodCallExpression)
            {
                var me = (MethodCallExpression)e;

                return Expression.Call(me.Object, me.Method, Expression.Invoke(current));
            }

            return e;
        }
    }
}
