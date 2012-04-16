using System;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;

namespace Lucene.Net.Linq
{
    public class QueryModelTranslator : QueryModelVisitorBase
    {
        private readonly Context context;
        private Query query;

        internal QueryModelTranslator(Context context)
        {
            this.context = context;
        }

        public Query Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
            
            return query ?? new MatchAllDocsQuery();
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionTreeVisitor(context);
            visitor.VisitExpression(whereClause.Predicate);

            if (query == null)
            {
                query = visitor.Query;
                return;
            }

            var bQuery = new BooleanQuery();
            bQuery.Add(query, BooleanClause.Occur.MUST);
            bQuery.Add(visitor.Query, BooleanClause.Occur.MUST);

            query = bQuery;
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            base.VisitOrderByClause(orderByClause, queryModel, index);
        }
    }
}