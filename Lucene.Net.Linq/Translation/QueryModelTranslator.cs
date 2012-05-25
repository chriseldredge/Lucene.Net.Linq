using System;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Translation.ResultOperatorHandlers;
using Lucene.Net.Linq.Translation.TreeVisitors;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Translation
{
    internal class QueryModelTranslator : QueryModelVisitorBase
    {
        private static readonly ResultOperatorRegistry resultOperators = ResultOperatorRegistry.CreateDefault();

        private readonly Context context;
        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly LuceneQueryModel model;

        internal QueryModelTranslator(Context context, IFieldMappingInfoProvider fieldMappingInfoProvider)
        {
            this.context = context;
            this.fieldMappingInfoProvider = fieldMappingInfoProvider;
            this.model = new LuceneQueryModel();
        }

        public void Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
        }

        public LuceneQueryModel Model
        {
            get { return model; }
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            var handler = resultOperators.GetItem(resultOperator.GetType());

            if (handler != null)
            {
                handler.Accept(resultOperator, model);
            }
            else
            {
                model.ApplyUnsupported(resultOperator);
            }

            base.VisitResultOperator(resultOperator, queryModel, index);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionTreeVisitor(context, fieldMappingInfoProvider);
            visitor.VisitExpression(whereClause.Predicate);

            model.AddQuery(visitor.Query);
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            foreach (var ordering in orderByClause.Orderings)
            {
                if (ordering.Expression is LuceneOrderByRelevanceExpression)
                {
                    model.AddSortField(SortField.FIELD_SCORE);
                    continue;
                }

                var field = (LuceneQueryFieldExpression)ordering.Expression;
                var mapping = fieldMappingInfoProvider.GetMappingInfo(field.FieldName);
                var reverse = ordering.OrderingDirection == OrderingDirection.Desc;

                if (mapping.SortFieldType >= 0)
                {
                    model.AddSortField(new SortField(mapping.FieldName, mapping.SortFieldType, reverse));    
                }
                else
                {
                    model.AddSortField(new SortField(mapping.FieldName, GetCustomSort(mapping), reverse));
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

            throw new NotSupportedException("Unsupported sort field type (does not implement IComparable): " + propertyType);
        }
    }
}