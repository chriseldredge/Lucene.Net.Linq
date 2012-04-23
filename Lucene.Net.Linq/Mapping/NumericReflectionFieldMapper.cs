using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    public class NumericReflectionFieldMapper<T> : ReflectionFieldMapper<T>
    {
        private readonly TypeConverter typeToValueTypeConverter;
        private readonly int precisionStep;

        public NumericReflectionFieldMapper(PropertyInfo propertyInfo, bool store, TypeConverter typeToValueTypeConverter, TypeConverter valueTypeToStringConverter, string field, int precisionStep)
            : base(propertyInfo, store, IndexMode.Analyzed, valueTypeToStringConverter, field)
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

            if (typeToValueTypeConverter.CanConvertTo(typeof(long)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(long));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(int)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(int));
            }
            if (converter.CanConvertTo(typeof(double)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(double));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(float)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(float));
            }

            return value;
        }
    }
}