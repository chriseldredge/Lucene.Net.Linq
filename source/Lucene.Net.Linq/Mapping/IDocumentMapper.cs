using Lucene.Net.Documents;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Converts objects of type <typeparamref name="T"/> to
    /// <see cref="Document"/> and back. Also creates
    /// <see cref="IDocumentKey"/>s to track, update
    /// or delete documents by key.
    /// </summary>
    public interface IDocumentMapper<in T> : IFieldMappingInfoProvider
    {
        /// <summary>
        /// Hydrates the properties on the target type using fields
        /// in the Lucene.Net Document.
        /// </summary>
        void ToObject(Document source, IQueryExecutionContext context, T target);

        /// <summary>
        /// Transfers property values on the source object
        /// to fields on the Lucene.Net Document.
        /// </summary>
        void ToDocument(T source, Document target);

        /// <summary>
        /// Create a composite key representing a unique
        /// identity for the document.
        /// </summary>
        IDocumentKey ToKey(T source);

        /// <summary>
        /// Compare two instances of <typeparamref name="T"/>
        /// to determine if they are considered equal. This
        /// method is used to detect modified objects in a
        /// <see cref="ISession{T}"/> to determine which
        /// objects are dirty and need to be updated during
        /// commit.
        /// </summary>
        /// <remarks>
        /// This method has been replaced by <see cref="IDocumentModificationDetector{T}.IsModified"/> 
        /// in 3.2 and may be removed in future versions.
        /// </remarks>
        bool Equals(T item1, T item2);

        /// <summary>
        /// Called before a search is executed to allow
        /// customizations to be applied on the <see cref="Searcher"/>,
        /// <see cref="Query"/> and <see cref="Filter"/>.
        /// </summary>
        void PrepareSearchSettings(IQueryExecutionContext context);

        /// <summary>
        /// Gets an analyzer to be used for preparing queries
        /// and writing documents.
        /// </summary>
        PerFieldAnalyzer Analyzer { get; }
    }
}