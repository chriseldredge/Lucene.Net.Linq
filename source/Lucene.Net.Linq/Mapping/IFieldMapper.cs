using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
    internal interface IFieldMapper<in T> : IFieldMappingInfo
    {
        void CopyFromDocument(Document source, float score, T target);
        void CopyToDocument(T source, Document target);
        object GetPropertyValue(T source);
    }
}