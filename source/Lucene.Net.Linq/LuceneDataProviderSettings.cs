using System;
using Lucene.Net.Index;
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
            DeletionPolicy = new KeepOnlyLastCommitDeletionPolicy();
            MaxFieldLength = IndexWriter.MaxFieldLength.UNLIMITED;
            MergeFactor = LogMergePolicy.DEFAULT_MERGE_FACTOR;
            RAMBufferSizeMB = IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB;
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

        /// <summary>
        /// Specifies the <see cref="IndexDeletionPolicy"/> of the <see cref="IndexWriter"/>.
        /// Default: <see cref="KeepOnlyLastCommitDeletionPolicy"/>.
        /// </summary>
        public IndexDeletionPolicy DeletionPolicy { get; set; }

        /// <summary>
        /// Specifies the <see cref="IndexWriter.MaxFieldLength"/> of the <see cref="IndexWriter"/>.
        /// Default: <see cref="IndexWriter.MaxFieldLength.UNLIMITED"/>.
        /// </summary>
        public IndexWriter.MaxFieldLength MaxFieldLength { get; set; }

        /// <summary>
        /// Specifies the merge factor when using <see cref="LogMergePolicy"/> or subclass.
        /// Default: 10 (from <see cref="LogMergePolicy.DEFAULT_MERGE_FACTOR"/>.
        /// </summary>
        /// <seealso cref="IndexWriter.MergeFactor"/>
        public int MergeFactor { get; set; }

        /// <summary>
        /// Specifies the RAM buffer size of the <see cref="IndexWriter"/> in megabytes.
        /// Default: 16.0 (from <see cref="IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB"/>.
        /// </summary>
        public double RAMBufferSizeMB { get; set; }

        /// <summary>
        /// A function that creates a <see cref="MergePolicy"/> for use with a <see cref="IndexWriter"/>.
        /// Default: <c>null</c>, which causes <see cref="IndexWriter"/> to use <see cref="LogByteSizeMergePolicy"/>.
        /// </summary>
        public Func<IndexWriter, MergePolicy> MergePolicyBuilder { get; set; }
    }
}
