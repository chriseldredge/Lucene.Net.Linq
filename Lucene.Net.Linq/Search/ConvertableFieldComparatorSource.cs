using System;
using System.ComponentModel;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Search
{
    public class ConvertableFieldComparatorSource : FieldComparatorSource
    {
        private readonly Type type;
        private readonly IFieldMappingInfo fieldMappingInfo;

        public ConvertableFieldComparatorSource(Type type, IFieldMappingInfo fieldMappingInfo)
        {
            this.type = type;
            this.fieldMappingInfo = fieldMappingInfo;
        }

        public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
        {
            var genericType = typeof(ConvertableFieldComparator<,>).MakeGenericType(type, typeof(string));
            var ctr = genericType.GetConstructor(new[] { typeof(int), typeof(string), typeof(TypeConverter) });
            return (FieldComparator)ctr.Invoke(new object[] { numHits, fieldname, fieldMappingInfo.Converter });
        }

        public class ConvertableFieldComparator<TComparable, TBackingField> : FieldComparator<TComparable> where TComparable : IComparable
        {
            private readonly TypeConverter converter;

            public ConvertableFieldComparator(int numHits, string field, TypeConverter converter)
                : base(numHits, field)
            {
                this.converter = converter;
            }

            protected override TComparable[] GetCurrentReaderValues(IndexReader reader, int docBase)
            {
                // TODO: handle non-string TBackingField
                var strings = FieldCache_Fields.DEFAULT.GetStrings(reader, field);
                return strings.Select(s => s == null ? default(TComparable) : converter.ConvertFrom(s)).Cast<TComparable>().ToArray();
            }
        }
    }
}