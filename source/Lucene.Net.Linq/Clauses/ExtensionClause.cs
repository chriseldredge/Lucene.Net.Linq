using System;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Clauses
{
    internal abstract class ExtensionClause<T> : IBodyClause where T : Expression
    {
        protected T expression;

        internal ExtensionClause(T expression)
        {
            this.expression = expression;
        }

        public void TransformExpressions(Func<Expression, Expression> transformation)
        {
            expression = transformation(expression) as T;
        }

        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index)
        {
            var customVisitor = visitor as ILuceneQueryModelVisitor;
            if (customVisitor == null) return;

            Accept(customVisitor, queryModel, index);
        }

        public abstract IBodyClause Clone(CloneContext cloneContext);

        protected abstract void Accept(ILuceneQueryModelVisitor visitor, QueryModel queryModel, int index);
    }
}