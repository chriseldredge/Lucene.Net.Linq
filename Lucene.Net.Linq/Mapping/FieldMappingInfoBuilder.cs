using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    internal class FieldMappingInfoBuilder
    {
        internal static IFieldMapper<T> Build<T>(PropertyInfo p)
        {
            var metadata = p.GetCustomAttribute<FieldAttribute>(true);
            var numericFieldAttribute = p.GetCustomAttribute<NumericFieldAttribute>(true);
            var type = p.PropertyType;

            var isCollection = IsCollection(p.PropertyType);

            if (isCollection)
            {
                type = p.PropertyType.GetGenericArguments()[0];
            }

            ReflectionFieldMapper<T> mapper;

            if (numericFieldAttribute != null)
            {
                mapper = BuildNumeric<T>(p, type, numericFieldAttribute);
            }
            else
            {
                mapper = BuildPrimitive<T>(p, type, metadata);
            }

            return isCollection ? new CollectionReflectionFieldMapper<T>(mapper, type) : mapper;
        }

        private static bool IsCollection(Type type)
        {
            return type.IsGenericType &&
                   typeof (IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition());
        }

        private static ReflectionFieldMapper<T> BuildPrimitive<T>(PropertyInfo p, Type type, FieldAttribute metadata)
        {
            var fieldName = (metadata != null ? metadata.Field : null) ?? p.Name;
            var converter = GetConverter(p, type, metadata);
            var store = metadata != null ? metadata.Store : true;
            var index = metadata != null ? metadata.IndexMode : IndexMode.Analyzed;


            return new ReflectionFieldMapper<T>(p, store, index, converter, fieldName);
        }

        private static ReflectionFieldMapper<T> BuildNumeric<T>(PropertyInfo p, Type type, NumericFieldAttribute metadata)
        {
            var fieldName = metadata.Field ?? p.Name;
            var typeToValueTypeConverter = (TypeConverter) null;
            var valueTypeToStringConverter = (TypeConverter)null;

            if (metadata.Converter != null)
            {
                typeToValueTypeConverter = (TypeConverter)Activator.CreateInstance(metadata.Converter);
                valueTypeToStringConverter = GetConverterConverter(typeToValueTypeConverter);
            }
            else
            {
                valueTypeToStringConverter = GetConverter(p, type, null);
            }
            

            return new NumericReflectionFieldMapper<T>(p, metadata.Store, typeToValueTypeConverter, valueTypeToStringConverter, fieldName,
                                                       metadata.PrecisionStep);
        }

        private static TypeConverter GetConverterConverter(TypeConverter typeToValueTypeConverter)
        {
            if (typeToValueTypeConverter.CanConvertTo(typeof(long)))
            {
                return TypeDescriptor.GetConverter(typeof (long));
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

        private static TypeConverter GetConverter(PropertyInfo p, Type type, BaseFieldAttribute metadata)
        {
            if (metadata != null && metadata.Converter != null)
            {
                return (TypeConverter)Activator.CreateInstance(metadata.Converter);
            }

            if (p.PropertyType == typeof(string)) return null;

            var converter = TypeDescriptor.GetConverter(type);

            if (converter == null || !converter.CanConvertFrom(typeof(string)))
            {
                throw new NotSupportedException("Property " + p.Name + " of type " + p.PropertyType + " cannot be converted from " + typeof(string));
            }
            return converter;
        }
    }
}