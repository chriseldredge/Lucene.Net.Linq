using System;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Provides access to statistics about queries via <see cref="LuceneMethods.CaptureStatistics{T}"/>.
    /// </summary>
    public class LuceneQueryStatistics
    {
        private readonly int totalHits;
        private readonly Query query;
        private readonly Filter filter;
        private readonly Sort sort;
        private readonly TimeSpan elapsedPreparationTime;
        private readonly TimeSpan elapsedSearchTime;
        private readonly TimeSpan elapsedRetrievalTime;
        private readonly int skippedHits;
        private readonly int retrievedDocuments;

        public LuceneQueryStatistics(Query query, Filter filter, Sort sort, TimeSpan elapsedPreparationTime, TimeSpan elapsedSearchTime, TimeSpan elapsedRetrievalTime, int totalHits, int skippedHits, int retrievedDocuments)
        {
            this.totalHits = totalHits;
            this.query = query;
            this.filter = filter;
            this.sort = sort;
            this.elapsedPreparationTime = elapsedPreparationTime;
            this.elapsedSearchTime = elapsedSearchTime;
            this.elapsedRetrievalTime = elapsedRetrievalTime;
            this.skippedHits = skippedHits;
            this.retrievedDocuments = retrievedDocuments;
        }
        
        /// <summary>
        /// The Query (generally a complex <see cref="BooleanQuery"/> or <see cref="MatchAllDocsQuery"/>)
        /// that was executed on <see cref="Searcher.Search(Lucene.Net.Search.Query,Lucene.Net.Search.Filter,int,Lucene.Net.Search.Sort)"/>
        /// </summary>
        public Query Query
        {
            get { return query; }
        }

        /// <summary>
        /// The Filter (null when <see cref="LuceneDataProviderSettings.EnableMultipleEntities"/> is false)
        /// that was executed on <see cref="Searcher.Search(Lucene.Net.Search.Query,Lucene.Net.Search.Filter,int,Lucene.Net.Search.Sort)"/>
        /// </summary>
        public Filter Filter
        {
            get { return filter; }
        }

        /// <summary>
        /// The Sort that was executed on <see cref="Searcher.Search(Lucene.Net.Search.Query,Lucene.Net.Search.Filter,int,Lucene.Net.Search.Sort)"/>
        /// </summary>
        public Sort Sort
        {
            get { return sort; }
        }

        /// <summary>
        /// Returns the total amount of time taken to translate the LINQ expression tree into a Lucene Query.
        /// </summary>
        public TimeSpan ElapsedPreparationTime
        {
            get { return elapsedPreparationTime; }
        }

        /// <summary>
        /// Returns the total amount of time spent in <see cref="Searcher.Search(Lucene.Net.Search.Query,Lucene.Net.Search.Filter,int,Lucene.Net.Search.Sort)"/>
        /// </summary>
        public TimeSpan ElapsedSearchTime
        {
            get { return elapsedSearchTime; }
        }

        /// <summary>
        /// Returns the total amount of time spent converting <see cref="Document"/> and enumerating projected results.
        /// </summary>
        public TimeSpan ElapsedRetrievalTime
        {
            get { return elapsedRetrievalTime; }
        }

        /// <summary>
        /// Returns the total hits that matched the query, including items that were not enumerated
        /// due to <c>Skip</c> and <c>Take</c>.
        /// </summary>
        public int TotalHits
        {
            get { return totalHits; }
        }

        /// <summary>
        /// Returns the number of hits that were skipped by <see cref="Enumerable.Skip{TSource}"/>
        /// </summary>
        public int SkippedHits
        {
            get { return skippedHits; }
        }

        /// <summary>
        /// Returns the number of hits that were retrieved. This will generally be the lesser
        /// of total hits or limit imposed by <see cref="Enumerable.Take{TSource}"/>.
        /// </summary>
        public int RetrievedDocuments
        {
            get { return retrievedDocuments; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("LuceneQueryStatistics { ");
            sb.Append("TotalHits: " + TotalHits);
            sb.Append(", Skipped: " + SkippedHits);
            sb.Append(", Retrieved: " + RetrievedDocuments);
            sb.Append(", ElapsedPreparationTime: " + ElapsedPreparationTime);
            sb.Append(", ElapsedSearchTime: " + ElapsedSearchTime);
            sb.Append(", ElapsedRetrievalTime: " + ElapsedRetrievalTime);
            sb.Append(", Query: " + Query);
            sb.Append(" }");

            return sb.ToString();
        }
    }
}