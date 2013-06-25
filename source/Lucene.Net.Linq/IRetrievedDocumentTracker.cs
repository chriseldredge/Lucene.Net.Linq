namespace Lucene.Net.Linq
{
    internal interface IRetrievedDocumentTracker<T>
    {
        void TrackDocument(T item, T hiddenCopy);
        bool TryGetTrackedDocument(T item, out T tracked);
        bool IsMarkedForDeletion(T item);
    }
}