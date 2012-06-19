using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq.Tests
{
    public class FakeFieldMappingInfo : IFieldMappingInfo
    {
        public string FieldName { get; set; }
        public TypeConverter Converter { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public string ConvertToQueryExpression(object value)
        {
            return value.ToString();
        }

        public bool IsNumericField
        {
            get { return true; }
        }

        public int SortFieldType
        {
            get { return -1; }
        }

        public bool CaseSensitive { get; set; }
    }
}