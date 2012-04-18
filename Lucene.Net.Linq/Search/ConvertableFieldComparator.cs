using System;
using System.ComponentModel;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    public class ConvertableFieldComparator<TComparable, TBackingField> : FieldComparator<TComparable> where TComparable : IComparable
    {
        private readonly TypeConverter converter;

        public ConvertableFieldComparator(int numHits, string field) : base(numHits, field)
        {
            converter = TypeDescriptor.GetConverter(typeof (TComparable));

            if (converter == null || !converter.CanConvertFrom(typeof(TBackingField)))
            {
                throw new ArgumentException("Generic type parameter " + typeof(TComparable) + " cannot be converted from " + typeof(TBackingField));
            }
        }

        protected override TComparable[] GetCurrentReaderValues(IndexReader reader, int docBase)
        {
            // TODO: handle non-string TBackingField
            var strings = FieldCache_Fields.DEFAULT.GetStrings(reader, field);

            return strings.Select(s => converter.ConvertFrom(s)).Cast<TComparable>().ToArray();
        }
    }
}