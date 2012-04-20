using System;
using System.ComponentModel;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    public class ConvertableFieldComparatorSource : FieldComparatorSource
    {
        private readonly Type type;

        public ConvertableFieldComparatorSource(Type type)
        {
            this.type = type;
        }

        public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
        {
            var genericType = typeof(ConvertableFieldComparator<,>).MakeGenericType(type, typeof(string));
            var ctr = genericType.GetConstructor(new[] { typeof(int), typeof(string) });
            return (FieldComparator)ctr.Invoke(new object[] { numHits, fieldname });
        }

        public class ConvertableFieldComparator<TComparable, TBackingField> : FieldComparator<TComparable> where TComparable : IComparable
        {
            private readonly TypeConverter converter;

            public ConvertableFieldComparator(int numHits, string field)
                : base(numHits, field)
            {
                converter = TypeDescriptor.GetConverter(typeof(TComparable));

                if (converter == null || !converter.CanConvertFrom(typeof(TBackingField)))
                {
                    throw new ArgumentException("Generic type parameter " + typeof(TComparable) + " cannot be converted from " + typeof(TBackingField));
                }
            }

            protected override TComparable[] GetCurrentReaderValues(IndexReader reader, int docBase)
            {
                // TODO: handle non-string TBackingField
                var strings = FieldCache_Fields.DEFAULT.GetStrings(reader, field);
                var longs = FieldCache_Fields.DEFAULT.GetLongs(reader, field);
                return strings.Select(s => s == null ? default(TComparable) : converter.ConvertFrom(s)).Cast<TComparable>().ToArray();
            }
        }
    }
}