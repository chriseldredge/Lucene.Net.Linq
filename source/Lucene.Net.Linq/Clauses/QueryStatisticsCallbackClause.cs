using System;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Clauses
{
    internal class QueryStatisticsCallbackClause : ExtensionClause<ConstantExpression>
    {
        public QueryStatisticsCallbackClause(ConstantExpression expression)
            : base(expression)
        {
        }

        public Action<LuceneQueryStatistics> Callback
        {
            get { return (Action<LuceneQueryStatistics>) expression.Value; }
        }

        protected override void Accept(ILuceneQueryModelVisitor visitor, QueryModel queryModel, int index)
        {
            visitor.VisitQueryStatisticsCallbackClause(this, queryModel, index);
        }

        public override IBodyClause Clone(CloneContext cloneContext)
        {
            return new QueryStatisticsCallbackClause(expression);
        }
    }
}