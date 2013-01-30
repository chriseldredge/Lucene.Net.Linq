using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.StreamedData;

namespace Lucene.Net.Linq
{
    internal class LuceneQueryModel
    {
        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query;
        private Delegate customScoreFunction;

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

        public Filter Filter { get; set; }
        public int MaxResults { get; set; }
        public int SkipResults { get; set; }
        public bool Last { get; set; }
        public bool Aggregate { get; set; }

        public Expression SelectClause { get; set; }
        public StreamedSequenceInfo OutputDataInfo { get; set; }
        public ResultOperatorBase ResultSetOperator { get; private set; }

        public object DocumentTracker { get; set; }

        public void AddQuery(Query additionalQuery)
        {
            if (query == null)
            {
                query = additionalQuery;
                return;
            }

            var bQuery = new BooleanQuery();
            bQuery.Add(query, Occur.MUST);
            bQuery.Add(additionalQuery, Occur.MUST);

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

            if (Filter != null)
            {
                sb.Append(" Filter: " + Filter);
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
            var propertyType = fieldMappingInfo.PropertyType;

            if (typeof(IComparable).IsAssignableFrom(propertyType))
            {
                return new NonGenericConvertableFieldComparatorSource(propertyType, fieldMappingInfo.Converter);
            }

            if (typeof (IComparable<>).MakeGenericType(propertyType).IsAssignableFrom(propertyType))
            {
                return new GenericConvertableFieldComparatorSource(propertyType, fieldMappingInfo.Converter);
            }

            throw new NotSupportedException("Unsupported sort field type (does not implement IComparable): " + propertyType);
        }

        public void AddBoostFunction(LambdaExpression expression)
        {
            var scoreFunction = expression.Compile();

            if (customScoreFunction != null)
            {
                scoreFunction = Delegate.Combine(customScoreFunction, scoreFunction);
            }

            customScoreFunction = scoreFunction;
        }

        public Func<TDocument, float> GetCustomScoreFunction<TDocument>()
        {
            if (customScoreFunction == null) return null;

            var invocationList = customScoreFunction.GetInvocationList().Cast<Func<TDocument, float>>().ToArray();

            if (invocationList.Length == 1)
            {
                return invocationList[0];
            }

            return delegate(TDocument document)
            {
                var score = 1.0f;
                foreach (var func in invocationList)
                {
                    score = score * func(document);
                }
                return score;
            };
        }
    }
}