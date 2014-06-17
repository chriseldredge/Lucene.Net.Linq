using Lucene.Net.Linq.Clauses;
using Remotion.Linq;

namespace Lucene.Net.Linq
{
    internal interface ILuceneQueryModelVisitor : IQueryModelVisitor
    {
        void VisitBoostClause(BoostClause clause, QueryModel queryModel, int index);
        void VisitTrackRetrievedDocumentsClause(TrackRetrievedDocumentsClause clause, QueryModel queryModel, int index);
        void VisitQueryStatisticsCallbackClause(QueryStatisticsCallbackClause clause, QueryModel queryModel, int index);
    }
}