using Lucene.Net.Analysis;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Maps Lucene.Net <see cref="Field"/>s onto instances
    /// of <typeparamref name="T"/>.
    /// </summary>
    public interface IFieldMapper<in T> : IFieldMappingInfo
    {
        /// <summary>
        /// Retrieve <see cref="Field"/> or other metadata
        /// from <paramref name="source"/> and <paramref name="context"/>
        /// and apply to <paramref name="target"/>.
        /// </summary>
        void CopyFromDocument(Document source, IQueryExecutionContext context, T target);

        /// <summary>
        /// Convert a Property or other data on an instance
        /// of <paramref name="source"/> into a <see cref="Field"/>
        /// on the <paramref name="target"/>.
        /// </summary>
        void CopyToDocument(T source, Document target);

        /// <summary>
        /// Retrieve a value from <paramref name="source"/>
        /// for the purposes of constructing an <see cref="IDocumentKey"/>
        /// or comparing instances of <typeparamref name="T"/>
        /// to detect dirty objects.
        /// </summary>
        object GetPropertyValue(T source);

        /// <summary>
        /// Gets the Analyzer to be used for indexing this field
        /// or parsing queries on this field.
        /// </summary>
        Analyzer Analyzer { get; }
    }
}