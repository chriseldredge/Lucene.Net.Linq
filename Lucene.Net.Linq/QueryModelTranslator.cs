using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;

namespace Lucene.Net.Linq
{
    public class QueryModelTranslator : QueryModelVisitorBase
    {
        private readonly Context context;
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query = new MatchAllDocsQuery();
        private int maxResults = int.MaxValue;
        private int skipResults;

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

        public int MaxResults
        {
            get { return maxResults; }
        }

        public int SkipResults
        {
            get { return skipResults; }
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            //TODO: resultOperator.ExecuteInMemory() on unsupported ones.

            if (resultOperator is TakeResultOperator)
            {
                var take = (TakeResultOperator) resultOperator;
                maxResults = Math.Min(take.GetConstantCount(), maxResults);
            }

            if (resultOperator is SkipResultOperator)
            {
                var skip = (SkipResultOperator)resultOperator;
                var additionalSkip = skip.GetConstantCount();
                skipResults += additionalSkip;

                if (maxResults != int.MaxValue)
                {
                    maxResults -= additionalSkip;
                }
            }

            base.VisitResultOperator(resultOperator, queryModel, index);
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

                var sortType = GetSortType(field.Type);

                if (sortType >= 0)
                {
                    sorts.Add(new SortField(field.FieldName, sortType, reverse));    
                }
                else
                {
                    sorts.Add(new SortField(field.FieldName, GetCustomSort(field.Type), reverse));
                }
            }
        }

        private FieldComparatorSource GetCustomSort(Type type)
        {
            if (typeof(IComparable).IsAssignableFrom(type))
            {
                return new ConvertableFieldComparatorSource(type);
            }

            throw new NotSupportedException("Unsupported sort field type: " + type);
        }

        private static int GetSortType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            type = (type == typeof (DateTimeOffset) || type == typeof (DateTime)) ? typeof (long) : type;

            if (type == typeof(string))
                return SortField.STRING;
            if (type == typeof(int) || type == typeof(bool))
                return SortField.INT;
            if (type == typeof(long))
                return SortField.LONG;

            return -1;
        }
    }
}