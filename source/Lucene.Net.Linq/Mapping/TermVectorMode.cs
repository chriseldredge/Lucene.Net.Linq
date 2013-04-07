using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    /// <see cref="Field.TermVector"/>
    public enum TermVectorMode
    {
        No = Field.TermVector.NO,
        Yes = Field.TermVector.YES,
        WithOffsets = Field.TermVector.WITH_OFFSETS,
        WithPositions = Field.TermVector.WITH_POSITIONS,
        WithPositionsAndOffsets = Field.TermVector.WITH_POSITIONS_OFFSETS
    }
}