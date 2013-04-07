using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <see cref="Field.Index"/>
    public enum IndexMode
    {
        /// <see cref="Field.Index.NO"/>
        NotIndexed = Field.Index.NO,
        /// <see cref="Field.Index.ANALYZED"/>
        Analyzed = Field.Index.ANALYZED,
        /// <see cref="Field.Index.ANALYZED_NO_NORMS"/>
        AnalyzedNoNorms = Field.Index.ANALYZED_NO_NORMS,
        /// <see cref="Field.Index.NOT_ANALYZED"/>
        NotAnalyzed = Field.Index.NOT_ANALYZED,
        /// <see cref="Field.Index.NOT_ANALYZED_NO_NORMS"/>
        NotAnalyzedNoNorms = Field.Index.NOT_ANALYZED_NO_NORMS,
    }
}