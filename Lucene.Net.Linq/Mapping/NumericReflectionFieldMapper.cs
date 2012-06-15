using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    public class NumericReflectionFieldMapper<T> : ReflectionFieldMapper<T>
    {
        private static readonly IEnumerable<Type> supportedValueTypes = new List<Type>{typeof(long), typeof(int), typeof(double), typeof(float)};

        private readonly TypeConverter typeToValueTypeConverter;
        private readonly int precisionStep;

        public NumericReflectionFieldMapper(PropertyInfo propertyInfo, StoreMode store, TypeConverter typeToValueTypeConverter, TypeConverter valueTypeToStringConverter, string field, int precisionStep)
            : base(propertyInfo, store, IndexMode.Analyzed, valueTypeToStringConverter, field, false)
        {
            this.typeToValueTypeConverter = typeToValueTypeConverter;
            this.precisionStep = precisionStep;
        }

        public int PrecisionStep
        {
            get { return precisionStep; }
        }

        public override bool IsNumericField
        {
            get { return true; }
        }

        public override int SortFieldType
        {
            get
            {
                if (typeToValueTypeConverter == null)
                {
                    return propertyInfo.PropertyType.ToSortField();
                }

                var targetType = GetUnderlyingValueType();

                return targetType.ToSortField();

            }
        }

        protected internal override object ConvertFieldValue(Field field)
        {
            var value = base.ConvertFieldValue(field);

            if (typeToValueTypeConverter != null)
            {
                value = typeToValueTypeConverter.ConvertFrom(value);
            }

            return value;
        }

        public override void CopyToDocument(T source, Document target)
        {
            var value = propertyInfo.GetValue(source, null);

            target.RemoveFields(fieldName);

            if (value == null) return;

            value = ConvertToSupportedValueType(value);

            var numericField = new NumericField(fieldName, precisionStep, FieldStore, true);

            numericField.SetValue((ValueType)value);

            target.Add(numericField);
        }

        public override string ConvertToQueryExpression(object value)
        {
            value = ConvertToSupportedValueType(value);
            
            return ((ValueType) value).ToPrefixCoded();
        }

        private object ConvertToSupportedValueType(object value)
        {
            if (typeToValueTypeConverter == null) return value;

            var type = GetUnderlyingValueType();

            return type != null ? typeToValueTypeConverter.ConvertTo(value, type) : value;
        }

        private Type GetUnderlyingValueType()
        {
            return supportedValueTypes.FirstOrDefault(t => typeToValueTypeConverter.CanConvertTo(t));
        }
    }
}