using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    public abstract class FieldComparator<T> : FieldComparator
    {
        protected string field;
        protected T[] values;
        protected T[] currentReaderValues;
        protected T bottom;

        protected FieldComparator(int numHits, string field)
        {
            this.field = field;
            this.values = new T[numHits];
        }

        public override void Copy(int slot, int doc)
        {
            values[slot] = currentReaderValues[doc];
        }

        public override void SetBottom(int bottom)
        {
            this.bottom = values[bottom];
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            currentReaderValues = GetCurrentReaderValues(reader, docBase);
        }

        protected abstract T[] GetCurrentReaderValues(IndexReader reader, int docBase);
    }
}
