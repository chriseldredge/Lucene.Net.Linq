using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Provides mapping information for the properties
    /// of a given type and corresponding field metadata.
    /// </summary>
    public interface IFieldMappingInfoProvider
    {
        /// <summary>
        /// Returns the set of fields defined for the given document.
        /// </summary>
        IEnumerable<string> AllProperties { get; }

        /// <summary>
        /// Returns the set of property names used to compose
        /// a <see cref="IDocumentKey"/> for the document.
        /// </summary>
        IEnumerable<string> KeyProperties { get; }

        /// <summary>
        /// Returns detailed mapping info for a given property name.
        /// </summary>
        IFieldMappingInfo GetMappingInfo(string propertyName);

        /// <summary>
        /// Create a query that matches the pattern on any field.
        /// Used in conjunction with <see cref="LuceneMethods.AnyField{T}"/>
        /// </summary>
        Query CreateMultiFieldQuery(string pattern);
    }
}