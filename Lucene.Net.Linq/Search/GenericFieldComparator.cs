using System;

namespace Lucene.Net.Linq.Search
{
    public abstract class GenericFieldComparator<T> : FieldComparator<T> where T : IComparable<T>
    {
        protected GenericFieldComparator(int numHits, string field)
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
            get { return new ComparableWrapper(values[slot]); }
        }

        class ComparableWrapper : IComparable
        {
            private readonly T value;

            public ComparableWrapper(T value)
            {
                this.value = value;
            }

            public int CompareTo(object obj)
            {
                var other = (T)obj;
                return value.CompareTo(other);
            }
        }
    }
}