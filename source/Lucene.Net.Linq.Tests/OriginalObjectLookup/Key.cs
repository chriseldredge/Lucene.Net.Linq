using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Tests.OriginalObjectLookup
{
    internal class Key : IDocumentKey
    {
        private readonly string _key;
        public Key(string key)
        {
            _key = key;
        }


        public bool Equals(IDocumentKey other)
        {
            var otherKey = other as Key;
            if (otherKey != null)
            {
                return otherKey._key == this._key;
            }
            return false;
        }


        public Query ToQuery()
        {
            return new TermQuery(new Term("__key", this._key));
        }

        public bool Empty { get { return false; } }
    }
}