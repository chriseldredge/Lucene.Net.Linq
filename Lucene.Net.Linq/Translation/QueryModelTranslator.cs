using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Translation.TreeVisitors;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;

namespace Lucene.Net.Linq.Translation
{
    internal class QueryModelTranslator : QueryModelVisitorBase
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
                if (ordering.Expression is LuceneOrderByRelevanceExpression)
                {
                    sorts.Add(SortField.FIELD_SCORE);
                    continue;
                }

                var field = (LuceneQueryFieldExpression)ordering.Expression;
                var mapping = fieldMappingInfoProvider.GetMappingInfo(field.FieldName);
                var reverse = ordering.OrderingDirection == OrderingDirection.Desc;

                if (mapping.SortFieldType >= 0)
                {
                    sorts.Add(new SortField(mapping.FieldName, mapping.SortFieldType, reverse));    
                }
                else
                {
                    sorts.Add(new SortField(mapping.FieldName, GetCustomSort(mapping), reverse));
                }
            }
        }

        private FieldComparatorSource GetCustomSort(IFieldMappingInfo fieldMappingInfo)
        {
            var propertyType = fieldMappingInfo.PropertyInfo.PropertyType;
            if (typeof(IComparable).IsAssignableFrom(propertyType))
            {
                return new ConvertableFieldComparatorSource(propertyType, fieldMappingInfo.Converter);
            }

            throw new NotSupportedException("Unsupported sort field type: " + propertyType);
        }
    }
}