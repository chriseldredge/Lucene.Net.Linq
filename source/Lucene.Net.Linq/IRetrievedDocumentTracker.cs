using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq
{
    internal interface IRetrievedDocumentTracker<T>
    {
        void TrackDocument(IDocumentKey key, T item, Document document);
        bool TryGetTrackedDocument(IDocumentKey key, out T tracked);
        bool IsMarkedForDeletion(IDocumentKey key);
    }
}