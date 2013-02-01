using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Linq.Util;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Represents a unique key for a document
    /// </summary>
    public interface IDocumentKey : IEquatable<IDocumentKey>
    {
        /// <summary>
        /// Converts the key to a Lucene.Net <see cref="Query"/>
        /// that will match a unique document in the index.
        /// </summary>
        Query ToQuery(Analyzer analyzer, Version version);

        /// <summary>
        /// Flag indicating if the key is empty, meaning
        /// that no key fields are defined for the document.
        /// </summary>
        bool Empty { get; }
    }

    public class DocumentKey : IDocumentKey
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
            values.Apply(kvp => query.Add(ConvertToQueryExpression(kvp, analyzer, version), Occur.MUST));
            return query;
        }

        private Query ConvertToQueryExpression(KeyValuePair<string, object> kvp, Analyzer analyzer, Version version)
        {
            var mapping = mappings[kvp.Key];
            
            if (mapping.KeyConstraint != null)
            {
                return mapping.KeyConstraint;
            }

            var parser = new QueryParser(version, kvp.Key, analyzer);

            return Parse(parser, mapping.ConvertToQueryExpression(kvp.Value));
            
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
        
        public bool Equals(IDocumentKey other)
        {
            return Equals((object)other);
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