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
                mapper = NumericFieldMappingInfoBuilder.BuildNumeric<T>(p, type, numericFieldAttribute);
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

        internal static TypeConverter GetConverter(PropertyInfo p, Type type, BaseFieldAttribute metadata)
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