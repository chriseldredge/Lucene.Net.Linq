using System;
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
            var genericType = typeof (ConvertableFieldComparator<,>).MakeGenericType(type, typeof (string));
            var ctr = genericType.GetConstructor(new[] {typeof (int), typeof (string)});
            return (FieldComparator) ctr.Invoke(new object[] {numHits, fieldname});
        }
    }
}