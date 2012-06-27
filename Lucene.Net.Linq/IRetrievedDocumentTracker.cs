namespace Lucene.Net.Linq
{
    internal interface IRetrievedDocumentTracker<in T>
    {
        void TrackDocument(T item, T hiddenCopy);
    }
}