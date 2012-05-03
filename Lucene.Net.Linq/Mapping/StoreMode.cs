using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <see cref="Field.Store"/>
    public enum StoreMode
    {
        /// <see cref="Field.Store.YES"/>
        Yes,
        /// <see cref="Field.Store.NO"/>
        No,
        /// <see cref="Field.Store.COMPRESS"/>
        Compress
    }
}