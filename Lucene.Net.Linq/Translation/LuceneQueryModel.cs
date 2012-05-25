using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;

namespace Lucene.Net.Linq.Translation
{
    internal class LuceneQueryModel
    {
        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query;

        public LuceneQueryModel(IFieldMappingInfoProvider fieldMappingInfoProvider)
        {
            this.fieldMappingInfoProvider = fieldMappingInfoProvider;
            MaxResults = int.MaxValue;
        }

        public Query Query
        {
            get { return query ?? new MatchAllDocsQuery(); }
        }

        public Sort Sort
        {
            get { return sorts.Count > 0 ? new Sort(sorts.ToArray()) : new Sort(); }
        }

        public int MaxResults { get; set; }
        public int SkipResults { get; set; }
        public bool Last { get; set; }
        public bool Aggregate { get; set; }

        public Expression SelectClause { get; set; }
        public StreamedSequenceInfo OutputDataInfo { get; set; }
        public ResultOperatorBase ResultSetOperator { get; private set; }

        public void AddQuery(Query additionalQuery)
        {
            if (query == null)
            {
                query = additionalQuery;
                return;
            }

            var bQuery = new BooleanQuery();
            bQuery.Add(query, BooleanClause.Occur.MUST);
            bQuery.Add(additionalQuery, BooleanClause.Occur.MUST);

            query = bQuery;
        }

        public void ApplyUnsupported(ResultOperatorBase resultOperator)
        {
            ResultSetOperator = resultOperator;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Query.ToString());
            if (sorts.Count > 0)
            {
                sb.Append(" sort by ");
                sb.Append(Sort);
            }

            if (SkipResults > 0)
            {
                sb.Append(".Skip(");
                sb.Append(SkipResults);
                sb.Append(")");
            }

            if (MaxResults < int.MaxValue)
            {
                sb.Append(".Take(");
                sb.Append(MaxResults);
                sb.Append(")");
            }

            if (Last)
            {
                sb.Append(".Last()");
            }

            return sb.ToString();
        }

        public void ResetSorts()
        {
            sorts.Clear();
        }

        public void AddSort(Expression expression, OrderingDirection direction)
        {
            if (expression is LuceneOrderByRelevanceExpression)
            {
                sorts.Add(SortField.FIELD_SCORE);
                return;
            }

            var reverse = direction == OrderingDirection.Desc;
            string propertyName;

            if (expression is LuceneQueryFieldExpression)
            {
                var field = (LuceneQueryFieldExpression) expression;
                propertyName = field.FieldName;
            }
            else
            {
                var selector = (MemberExpression)expression;
                propertyName = selector.Member.Name;
            }

            var mapping = fieldMappingInfoProvider.GetMappingInfo(propertyName);

            if (mapping.SortFieldType >= 0)
            {
                sorts.Add(new SortField(mapping.FieldName, mapping.SortFieldType, reverse));
            }
            else
            {
                sorts.Add(new SortField(mapping.FieldName, GetCustomSort(mapping), reverse));
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