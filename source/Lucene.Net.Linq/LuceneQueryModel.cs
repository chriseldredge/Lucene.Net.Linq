using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Mapping;
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

        public event Action<LuceneQueryStatistics> OnCaptureQueryStatistics;

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
                if (direction == OrderingDirection.Desc)
                {
                    sorts.Add(new SortField(SortField.FIELD_SCORE.Field, SortField.FIELD_SCORE.Type, true));
                }
                else
                {
                    sorts.Add(SortField.FIELD_SCORE);
                }
                
                return;
            }

            var reverse = direction == OrderingDirection.Desc;
            string propertyName;

            if (expression is UnaryExpression)
            {
                var selector = (UnaryExpression)expression;
                expression = selector.Operand;
            }

            if (expression is LuceneQueryFieldExpression)
            {
                var field = (LuceneQueryFieldExpression) expression;
                propertyName = field.FieldName;
            }
            else if (expression is MemberExpression)
            {
                var selector = (MemberExpression)expression;
                propertyName = selector.Member.Name;
            }
            else
            {
                throw new ArgumentException("Unsupported sort expression type " + expression.GetType());
            }

            var mapping = fieldMappingInfoProvider.GetMappingInfo(propertyName);

            sorts.Add(mapping.CreateSortField(reverse));
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

        public void AddQueryStatisticsCallback(Action<LuceneQueryStatistics> callback)
        {
            OnCaptureQueryStatistics += callback;
        }

        public void RaiseCaptureQueryStatistics(LuceneQueryStatistics statistics)
        {
            if (OnCaptureQueryStatistics != null)
            {
                OnCaptureQueryStatistics(statistics);
            }
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
