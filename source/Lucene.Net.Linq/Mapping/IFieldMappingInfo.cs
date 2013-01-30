using System;
using System.ComponentModel;

namespace Lucene.Net.Linq.Mapping
{
    internal interface IFieldMappingInfo
    {
        string FieldName { get; }
        string PropertyName { get; }
        Type PropertyType { get; }
        TypeConverter Converter { get; }
        
        bool IsNumericField { get; }
        int SortFieldType { get; }
        bool CaseSensitive { get; }
        string KeyConstraint { get; }
        string ConvertToQueryExpression(object value);
    }
}