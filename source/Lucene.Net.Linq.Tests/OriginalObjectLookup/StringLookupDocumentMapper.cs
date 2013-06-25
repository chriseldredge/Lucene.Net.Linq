using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.OriginalObjectLookup
{
    internal class LookupDocumentMapper<T> : ReflectionDocumentMapper<T> 
    {
        private readonly Func<string, T> _findObject;
        private readonly Func<T, string> _findKey;

        public LookupDocumentMapper(Func<string, T> findObject, Func<T, string> findKey, Version version)
            : base(version)
        {
            _findObject = findObject;
            _findKey = findKey;
        }

        public LookupDocumentMapper(Func<string, T> findObject, Version version, Analyzer externalAnalyzer)
            : base(version, externalAnalyzer)
        {
            _findObject = findObject;
        }

        public override void ToDocument(T source, global::Lucene.Net.Documents.Document target)
        {
            base.ToDocument(source, target);
            target.Add(new Field("__key", _findKey(source), Field.Store.YES, Field.Index.NOT_ANALYZED));
        }

        public override IDocumentKey ToKey(T source)
        {
            return new Key(_findKey(source));
        }

        public override void ToObject(global::Lucene.Net.Documents.Document source,
                                      global::Lucene.Net.Linq.IQueryExecutionContext context, T target)
        {

            base.ToObject(source, context, target);
        }

        public T Create(Document source)
        {
            var id = source.GetField("__key").StringValue;
            return _findObject(id);
        }
    }

}