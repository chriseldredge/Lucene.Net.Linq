using System.Collections.Generic;

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
        IEnumerable<string> AllFields { get; }

        /// <summary>
        /// Returns the set of property names used to compose
        /// a <see cref="IDocumentKey"/> for the document.
        /// </summary>
        IEnumerable<string> KeyProperties { get; }

        /// <summary>
        /// Returns detailed mapping info for a given property name.
        /// </summary>
        IFieldMappingInfo GetMappingInfo(string propertyName);
    }
}