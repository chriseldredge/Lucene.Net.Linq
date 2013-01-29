using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <see cref="Field.Index"/>
    public enum IndexMode
    {
        /// <see cref="Field.Index.ANALYZED"/>
        Analyzed,
        /// <see cref="Field.Index.ANALYZED_NO_NORMS"/>
        AnalyzedNoNorms,
        /// <see cref="Field.Index.NOT_ANALYZED"/>
        NotAnalyzed,
        /// <see cref="Field.Index.NOT_ANALYZED_NO_NORMS"/>
        NotAnalyzedNoNorms,
        /// <see cref="Field.Index.NO"/>
        NotIndexed
    }
}