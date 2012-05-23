using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;

namespace Lucene.Net.Linq.Translation
{
    internal class LuceneQueryModel
    {
        private readonly IList<SortField> sorts = new List<SortField>();
        private Query query;

        public LuceneQueryModel()
        {
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

        public int MaxResults { get; private set; }
        public int SkipResults { get; private set; }
        public bool Last { get; private set; }
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

        public void AddSortField(SortField field)
        {
            sorts.Add(field);
        }

        public void ApplyTake(TakeResultOperator take)
        {
            MaxResults = Math.Min(take.GetConstantCount(), MaxResults);
        }

        public void ApplySkip(SkipResultOperator skip)
        {
            var additionalSkip = skip.GetConstantCount();
            SkipResults += additionalSkip;

            if (MaxResults != int.MaxValue)
            {
                MaxResults -= additionalSkip;
            }
        }

        public void ApplyFirst()
        {
            MaxResults = 1;
        }

        public void ApplyLast()
        {
            Last = true;
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
    }
}