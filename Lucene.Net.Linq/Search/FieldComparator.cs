using System;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    public abstract class FieldComparator<T> : FieldComparator where T : IComparable
    {
        protected readonly string field;
        protected readonly T[] values;
        private T[] currentReaderValues;
        private T bottom;

        protected FieldComparator(int numHits, string field)
        {
            this.field = field;
            values = new T[numHits];
        }

        public override void Copy(int slot, int doc)
        {
            values[slot] = currentReaderValues[doc];
        }

        public override void SetBottom(int bottom)
        {
            this.bottom = values[bottom];
        }

        public override int Compare(int slot1, int slot2)
        {
            return values[slot1].CompareTo(values[slot2]);
        }

        public override int CompareBottom(int doc)
        {
            return bottom.CompareTo(currentReaderValues[doc]);
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            currentReaderValues = GetCurrentReaderValues(reader, docBase);
        }

		public override IComparable this[int slot]
		{
			get { return values[slot]; }
		}

        protected abstract T[] GetCurrentReaderValues(IndexReader reader, int docBase);
    }
}
