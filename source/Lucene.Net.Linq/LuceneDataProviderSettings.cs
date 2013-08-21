using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Holds configuration settings that specify how the library behaves.
    /// </summary>
    public class LuceneDataProviderSettings
    {
        public LuceneDataProviderSettings()
        {
            EnableMultipleEntities = true;
        }

        /// <summary>
        /// <para>
        /// This setting controls whether searches performed on the IndexSearcher will
        /// include a <see cref="QueryWrapperFilter"/> that ensures that only documents
        /// that match <see cref="DocumentKeyAttribute"/> (or <see cref="ClassMap{T}.DocumentKey"/>)
        /// and have a non-blank field for each property that has <see cref="BaseFieldAttribute.Key"/>
        /// (or <see cref="ClassMap{T}.Key"/>).
        /// </para>
        /// <para>
        /// When enabled, entities of differing types can be safely stored in the same
        /// index. However, including this filter can severely reduce query execution performance.
        /// This setting is enabled by default to preserve backwards compatibility but may
        /// be disabled by default in a future release to provide better performance as
        /// the default case.
        /// </para>
        /// <para>
        /// Default: <c>true</c>
        /// </para>
        /// </summary>
        public bool EnableMultipleEntities { get; set; }
    }
}