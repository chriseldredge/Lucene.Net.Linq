using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq
{
    public class QueryModelTranslator : QueryModelVisitorBase
    {
        private readonly Context context;
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query = new MatchAllDocsQuery();
        
        internal QueryModelTranslator(Context context)
        {
            this.context = context;
        }

        public void Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
        }

        public Query Query
        {
            get { return query; }
        }

        public Sort Sort
        {
            get { return sorts.Count > 0 ? new Sort(sorts.ToArray()) : new Sort(); }
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
            foreach (var x in orderByClause.Orderings)
            {
                var field = (LuceneQueryFieldExpression)x.Expression;
                var reverse = x.OrderingDirection == OrderingDirection.Desc;
                sorts.Add(new SortField(field.FieldName, GetSortType(field.Type), reverse));
            }
        }

        private static int GetSortType(Type type)
        {
            if (type == typeof(string))
                return SortField.STRING;
            if (type == typeof(int))
                return SortField.INT;

            throw new NotSupportedException("Unsupported sort field type: " + type);
        }
    }
}