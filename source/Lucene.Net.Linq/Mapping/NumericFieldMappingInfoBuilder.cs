using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Linq.Converters;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    internal static class NumericFieldMappingInfoBuilder
    {
        internal static ReflectionFieldMapper<T> BuildNumeric<T>(PropertyInfo p, Type type, NumericFieldAttribute metadata)
        {
            var fieldName = metadata.Field ?? p.Name;
            var typeToValueTypeConverter = GetComplexTypeToScalarConverter(type, metadata);
            var valueTypeToStringConverter = (TypeConverter)null;

            if (typeToValueTypeConverter != null)
            {
                valueTypeToStringConverter = GetScalarToStringConverter(typeToValueTypeConverter);
            }
            else
            {
                valueTypeToStringConverter = FieldMappingInfoBuilder.GetConverter(p, type, null);
            }

            return new NumericReflectionFieldMapper<T>(p, metadata.Store, typeToValueTypeConverter, valueTypeToStringConverter, fieldName,
                                                       metadata.PrecisionStep, metadata.Boost);
        }

        private static TypeConverter GetComplexTypeToScalarConverter(Type type, NumericFieldAttribute metadata)
        {
            if (metadata.Converter != null)
            {
                return (TypeConverter)Activator.CreateInstance(metadata.Converter);
            }

            type = type.GetUnderlyingType();

            if (type == typeof(DateTime))
            {
                return new DateTimeToTicksConverter();
            }
            if (type == typeof(DateTimeOffset))
            {
                return new DateTimeOffsetToTicksConverter();
            }

            return null;
        }

        private static TypeConverter GetScalarToStringConverter(TypeConverter typeToValueTypeConverter)
        {
            if (typeToValueTypeConverter.CanConvertTo(typeof(long)))
            {
                return TypeDescriptor.GetConverter(typeof(long));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(int)))
            {
                return TypeDescriptor.GetConverter(typeof(int));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(double)))
            {
                return TypeDescriptor.GetConverter(typeof(double));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(float)))
            {
                return TypeDescriptor.GetConverter(typeof(float));
            }

            throw new NotSupportedException("TypeConverter of type " + typeToValueTypeConverter.GetType() + " does not convert values to any of long, int, double or float.");
        }
    }
}