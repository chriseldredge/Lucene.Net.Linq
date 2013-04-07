using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <see cref="Field.Store"/>
    public enum StoreMode
    {
        /// <see cref="Field.Store.YES"/>
        Yes = Field.Store.YES,
        /// <see cref="Field.Store.NO"/>
        No = Field.Store.NO
    }
}