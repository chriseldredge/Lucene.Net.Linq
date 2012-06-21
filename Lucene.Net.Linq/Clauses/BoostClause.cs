using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Translation;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Clauses
{
    internal class BoostClause : IBodyClause
    {
        private LambdaExpression boostFunction;

        internal BoostClause(LambdaExpression boostFunction)
        {
            this.boostFunction = boostFunction;
        }

        public LambdaExpression BoostFunction
        {
            get { return boostFunction; }
        }

        public void TransformExpressions(Func<Expression, Expression> transformation)
        {
            boostFunction = transformation(boostFunction) as LambdaExpression;
        }

        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index)
        {
            var customVisitor = visitor as ILuceneQueryModelVisitor;
            if (customVisitor == null) return;

            customVisitor.VisitBoostClause(this, queryModel, index);
        }

        public IBodyClause Clone(CloneContext cloneContext)
        {
            return new BoostClause(boostFunction);
        }

        public override string ToString()
        {
            return "boost " + boostFunction;
        }
    }
}
