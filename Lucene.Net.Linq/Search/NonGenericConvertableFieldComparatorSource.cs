using System;
using System.ComponentModel;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    internal class NonGenericConvertableFieldComparatorSource : FieldComparatorSource
    {
        private readonly Type type;
        private readonly TypeConverter converter;

        public NonGenericConvertableFieldComparatorSource(Type type, TypeConverter converter)
        {
            this.type = type;
            this.converter = converter;
        }

        public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
        {
            var genericType = typeof(NonGenericConvertableFieldComparator<>).MakeGenericType(type);
            var ctr = genericType.GetConstructor(new[] { typeof(int), typeof(string), typeof(TypeConverter) });
            return (FieldComparator)ctr.Invoke(new object[] { numHits, fieldname, converter });
        }

        public class NonGenericConvertableFieldComparator<TComparable> : NonGenericFieldComparator<TComparable> where TComparable : IComparable
        {
            private readonly TypeConverter converter;

            public NonGenericConvertableFieldComparator(int numHits, string field, TypeConverter converter)
                : base(numHits, field)
            {
                this.converter = converter;
            }

            protected override TComparable[] GetCurrentReaderValues(IndexReader reader, int docBase)
            {
                var strings = FieldCache_Fields.DEFAULT.GetStrings(reader, field);
                return strings.Select(s => s == null ? default(TComparable) : converter.ConvertFrom(s)).Cast<TComparable>().ToArray();
            }
        }
    }
}