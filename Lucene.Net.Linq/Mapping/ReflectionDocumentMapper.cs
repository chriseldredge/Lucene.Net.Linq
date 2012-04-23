using System;
using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    public interface IFieldMappingInfoProvider
    {
        IFieldMappingInfo GetMappingInfo(string propertyName);
    }

    public interface IDocumentMapper<in T> : IFieldMappingInfoProvider
    {
        void ToObject(Document source, T target);
    }

    internal class ReflectionDocumentMapper<T> : IDocumentMapper<T>
    {
        private readonly IDictionary<string, IFieldMapper<T>> fieldMap = new Dictionary<string, IFieldMapper<T>>();

        public ReflectionDocumentMapper() : this(typeof(T))
        {
        }

        public ReflectionDocumentMapper(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                if (p.GetCustomAttribute<IgnoreFieldAttribute>(true) != null)
                {
                    continue;
                }
                var mappingContext = FieldMappingInfoBuilder.Build<T>(p);
                fieldMap.Add(mappingContext.PropertyInfo.Name, mappingContext);
            }
        }

        public IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return fieldMap[propertyName];
        }

        public void ToObject(Document source, T target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyFromDocument(source, target);
            }
        }

        public void ToDocument(T source, Document target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyToDocument(source, target);
            }
        }
    }
}