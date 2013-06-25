using System.Collections.Generic;

namespace Lucene.Net.Linq
{
    internal interface IRetrievedDocumentTracker<T>
    {
        void TrackDocument(T item, IEnumerable<object> storedFieldValuesForChangeTracking);
        bool TryGetTrackedDocument(T item, out T tracked);
        bool IsMarkedForDeletion(T item);
    }
}