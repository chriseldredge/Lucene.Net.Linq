using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Provides context for a search being
    /// prepared or executed to <see cref="IDocumentMapper{T}"/>
    /// </summary>
    public interface IQueryExecutionContext
    {
        /// <summary>
        /// The phase that the query execution is
        /// currently in. When the value is
        /// <see cref="QueryExecutionPhase.Execute"/>,
        /// the properties <see cref="Hits"/>
        /// and <see cref="CurrentScoreDoc"/> will
        /// be null because they have not yet been
        /// constructed.
        /// </summary>
        QueryExecutionPhase Phase { get; }

        /// <summary>
        /// Provides access to all hits returned
        /// by the search.
        /// </summary>
        TopFieldDocs Hits { get; }

        /// <summary>
        /// Returns the current index in the
        /// array of hits.
        /// </summary>
        int CurrentHit { get; }

        /// <summary>
        /// Convenience method for returning
        /// the current ScoreDoc, which could
        /// also be retrieved by doing e.g.
        /// <c>Hits.ScoreDocs[CurrentHit]</c>.
        /// </summary>
        ScoreDoc CurrentScoreDoc { get; }
        
        /// <summary>
        /// Provides a reference to the searcher
        /// to allow custom implementations to
        /// enable additional features as needed.
        /// </summary>
        IndexSearcher Searcher { get; }

        /// <summary>
        /// Provides access to the query that will
        /// be executed, allowing custom implementations
        /// of <see cref="IDocumentMapper{T}"/> to
        /// customize it.
        /// </summary>
        Query Query { get; set; }

        /// <summary>
        /// Provides access to the filter that will 
        /// be applied, allowing custom implementations
        /// of <see cref="IDocumentMapper{T}"/> to
        /// customize it.
        /// 
        /// When <see cref="IFieldMappingInfoProvider.KeyProperties"/>
        /// has one or more entries, the filter will
        /// be initialized to match documents that
        /// have the corresponding fields or match
        /// specific criteria defined by <see cref="IFieldMappingInfo.KeyConstraint"/>
        /// </summary>
        Filter Filter { get; set; }
    }
}