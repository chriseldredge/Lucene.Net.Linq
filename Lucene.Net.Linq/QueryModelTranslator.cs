using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Mapping;
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
        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query;
        private int maxResults = int.MaxValue;
        private int skipResults;

        internal QueryModelTranslator(Context context, IFieldMappingInfoProvider fieldMappingInfoProvider)
        {
            this.context = context;
            this.fieldMappingInfoProvider = fieldMappingInfoProvider;
        }

        public void Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
        }

        public Query Query
        {
            get { return query ?? new MatchAllDocsQuery(); }
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

        public ResultOperatorBase ResultSetOperator { get; set; }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            //TODO: resultOperator.ExecuteInMemory() on unsupported ones.
            
            if (resultOperator is TakeResultOperator)
            {
                var take = (TakeResultOperator) resultOperator;
                maxResults = Math.Min(take.GetConstantCount(), maxResults);
            }
            else if (resultOperator is SkipResultOperator)
            {
                var skip = (SkipResultOperator)resultOperator;
                var additionalSkip = skip.GetConstantCount();
                skipResults += additionalSkip;

                if (maxResults != int.MaxValue)
                {
                    maxResults -= additionalSkip;
                }
            }
            else
            {
                ResultSetOperator = resultOperator;
            }

            base.VisitResultOperator(resultOperator, queryModel, index);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionTreeVisitor(context, fieldMappingInfoProvider);
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
            foreach (var ordering in orderByClause.Orderings)
            {
                var field = (LuceneQueryFieldExpression)ordering.Expression;
                var reverse = ordering.OrderingDirection == OrderingDirection.Desc;
                
                var sortType = GetSortType(field.Type);

                if (sortType >= 0)
                {
                    sorts.Add(new SortField(field.FieldName, sortType, reverse));    
                }
                else
                {
                    sorts.Add(new SortField(field.FieldName, GetCustomSort(field), reverse));
                }
            }
        }

        private FieldComparatorSource GetCustomSort(LuceneQueryFieldExpression expression)
        {
            if (typeof(IComparable).IsAssignableFrom(expression.Type))
            {
                return new ConvertableFieldComparatorSource(expression.Type, fieldMappingInfoProvider.GetMappingInfo(expression.FieldName));
            }

            throw new NotSupportedException("Unsupported sort field type: " + expression.Type);
        }

        private static int GetSortType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            // TODO: get from field mapping info
            type = (type == typeof (DateTimeOffset) || type == typeof (DateTime)) ? typeof (long) : type;

            if (type == typeof(string))
                return SortField.STRING;
            if (type == typeof(int))
                return SortField.INT;
            if (type == typeof(long))
                return SortField.LONG;

            return -1;
        }
    }
}