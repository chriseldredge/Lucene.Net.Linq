using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Util;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Mapping
{
    internal class DocumentKey
    {
        private readonly IDictionary<string, object> values;
        private readonly IDictionary<string, IFieldMappingInfo> mappings;

        public DocumentKey()
        {
        }

        public DocumentKey(IDictionary<IFieldMappingInfo, object> values)
        {
            this.values = new SortedDictionary<string, object>(values.ToDictionary(kv => kv.Key.FieldName, kv => kv.Value));
            this.mappings = new Dictionary<string, IFieldMappingInfo>(values.ToDictionary(kv => kv.Key.FieldName, kv => kv.Key));
        }

        public Query ToQuery(Analyzer analyzer, Version version)
        {
            if (Empty)
            {
                throw new InvalidOperationException("No key fields defined.");
            }

            var query = new BooleanQuery();
            values.Apply(kvp => query.Add(Parse(new QueryParser(version, kvp.Key, analyzer), ConvertToQueryExpression(kvp)), Occur.MUST));
            return query;
        }

        private string ConvertToQueryExpression(KeyValuePair<string, object> kvp)
        {
            var mapping = mappings[kvp.Key];
            return mapping.ConvertToQueryExpression(kvp.Value);
        }

        private Query Parse(QueryParser parser, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException("The key value for field " + parser.Field + " must not be blank.");
            }
            return parser.Parse(QueryParser.Escape(value));
        }

        public bool Equals(DocumentKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (Empty) return false;

            return values.SequenceEqual(other.values);
        }
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (DocumentKey)) return false;
            return Equals((DocumentKey) obj);
        }

        public override int GetHashCode()
        {
            if (Empty) return 0;

            unchecked
            {
                var hash = values.Count;
                values.Apply(kv => hash += kv.Key.GetHashCode() + (kv.Value != null ? kv.Value.GetHashCode() : 0));
                return hash;
            }
        }

        public bool Empty
        {
            get { return values == null || values.Count == 0; }
        }
    }
}