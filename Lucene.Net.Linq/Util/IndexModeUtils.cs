using System;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq.Util
{
    /// <summary>
    /// Converts pretty IndexMode.AnalyzedNoNorms to ugly Field.Index.ANALYZED_NO_NORMS.
    /// </summary>
    public static class IndexModeUtils
    {
        public static Field.Index ToFieldIndex(this IndexMode mode)
        {
            switch(mode)
            {
                case IndexMode.NotIndexed:
                    return Field.Index.NO;
                case IndexMode.Analyzed:
                    return Field.Index.ANALYZED;
                case IndexMode.AnalyzedNoNorms:
                    return Field.Index.ANALYZED_NO_NORMS;
                case IndexMode.NotAnalyzed:
                    return Field.Index.NOT_ANALYZED;
                case IndexMode.NotAnalyzedNoNorms:
                    return Field.Index.NOT_ANALYZED_NO_NORMS;
            }

            throw new ArgumentException("Unrecognized indexing mode " + mode, "mode");
        }
    }
}