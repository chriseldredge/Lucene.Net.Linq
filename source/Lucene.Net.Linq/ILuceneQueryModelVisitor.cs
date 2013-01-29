using Lucene.Net.Linq.Clauses;
using Remotion.Linq;

namespace Lucene.Net.Linq
{
    internal interface ILuceneQueryModelVisitor : IQueryModelVisitor
    {
        void VisitBoostClause(BoostClause boostClause, QueryModel queryModel, int index);
        void VisitTrackRetrievedDocumentsClause(TrackRetrievedDocumentsClause trackRetrievedDocumentsClause, QueryModel queryModel, int index);
    }
}