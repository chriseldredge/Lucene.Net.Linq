using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

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
        Query ToQuery();

        /// <summary>
        /// Flag indicating if the key is empty, meaning
        /// that no key fields are defined for the document.
        /// </summary>
        bool Empty { get; }

        /// <summary>
        /// Contains list of properties that are used for the key.
        /// </summary>
        IEnumerable<string> Properties { get; }

        /// <summary>
        /// Retrieves the value for a given property.
        /// </summary>
        object this[string property] { get; }
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
            this.values = new SortedDictionary<string, object>(values.ToDictionary(kv => kv.Key.FieldName, kv => kv.Value, StringComparer.Ordinal), StringComparer.Ordinal);
            this.mappings = values.ToDictionary(kv => kv.Key.FieldName, kv => kv.Key, StringComparer.Ordinal);
        }

        public Query ToQuery()
        {
            if (Empty)
            {
                throw new InvalidOperationException("No key fields defined.");
            }

            var query = new BooleanQuery();
            values.Apply(kvp => query.Add(ConvertToQueryExpression(kvp), Occur.MUST));
            return query;
        }

        public IEnumerable<string> Properties
        {
            get { return values.Keys; }
        }

        public object this[string property]
        {
            get { return values[property]; }
        }

        private Query ConvertToQueryExpression(KeyValuePair<string, object> kvp)
        {
            var mapping = mappings[kvp.Key];
            
            var term = mapping.ConvertToQueryExpression(kvp.Value);
            if (string.IsNullOrWhiteSpace(term))
            {
                throw new InvalidOperationException("Value for key field '" + kvp.Key + "' cannot be null or empty.");
            }
            return mapping.CreateQuery(term);
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