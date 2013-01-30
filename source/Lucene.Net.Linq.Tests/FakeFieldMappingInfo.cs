using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq.Tests
{
    public class FakeFieldMappingInfo : IFieldMappingInfo
    {
        public FakeFieldMappingInfo()
        {
            IsNumericField = true;
        }

        public string FieldName { get; set; }
        public TypeConverter Converter { get; set; }
        public bool IsNumericField { get; set; }

        public string PropertyName { get; set; }
        public Type PropertyType { get; set; }

        public string ConvertToQueryExpression(object value)
        {
            return value.ToString();
        }

        public int SortFieldType
        {
            get { return -1; }
        }

        public bool CaseSensitive { get; set; }
    }
}