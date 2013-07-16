using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// <para>
    /// Extension interface for <see cref="IFieldMapper{T}"/> to enable
    /// building <see cref="IDocumentKey"/> without needing to construct
    /// extra instances of the object being mapped.
    /// </para>
    /// <para>Since 3.2</para>
    /// </summary>
    public interface IDocumentFieldConverter
    {
        /// <summary>
        /// Retrieve a field from the given document and
        /// convert it to a value suitable for the given mapping.
        /// </summary>
        object GetFieldValue(Document document);
    }
}