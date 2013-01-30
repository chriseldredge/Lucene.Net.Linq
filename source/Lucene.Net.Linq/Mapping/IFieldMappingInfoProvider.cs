using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    internal interface IFieldMappingInfoProvider
    {
        IFieldMappingInfo GetMappingInfo(string propertyName);
        IEnumerable<string> AllFields { get; }
        IEnumerable<string> KeyProperties { get; }
    }

    internal interface IDocumentMapper<in T> : IFieldMappingInfoProvider
    {
        void ToObject(Document source, float score, T target);
        void ToDocument(T source, Document target);
        IDocumentKey ToKey(T source);
        bool Equals(T item1, T item2);
        bool EnableScoreTracking { get; }
    }
}