using System;

namespace Lucene.Net.Linq.Search
{
    public abstract class NonGenericFieldComparator<T> : FieldComparator<T> where T : IComparable
    {
        protected NonGenericFieldComparator(int numHits, string field)
            : base(numHits, field)
        {
        }

        public override int Compare(int slot1, int slot2)
        {
            return values[slot1].CompareTo(values[slot2]);
        }

        public override int CompareBottom(int doc)
        {
            return bottom.CompareTo(currentReaderValues[doc]);
        }

        public override IComparable this[int slot]
        {
            get { return values[slot]; }
        }
    }
}