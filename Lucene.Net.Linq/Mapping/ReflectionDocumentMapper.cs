using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    internal interface IFieldMappingInfoProvider
    {
        IFieldMappingInfo GetMappingInfo(string propertyName);
        IEnumerable<string> AllFields { get; }
    }

    internal interface IDocumentMapper<in T> : IFieldMappingInfoProvider
    {
        void ToObject(Document source, float score, T target);
        void ToDocument(T source, Document target);
        DocumentKey ToKey(T source);
        bool Equals(T item1, T item2);
        bool EnableScoreTracking { get; }
    }

    internal class ReflectionDocumentMapper<T> : IDocumentMapper<T>
    {
        private readonly IDictionary<string, IFieldMapper<T>> fieldMap = new Dictionary<string, IFieldMapper<T>>();
        private readonly List<IFieldMapper<T>> keyFields;

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

            var keyProps = from p in props
                           let a = p.GetCustomAttribute<BaseFieldAttribute>(true)
                           where a != null && a.Key
                           select p;

            keyFields = keyProps.Select(kp => fieldMap[kp.Name]).ToList();
        }

        public IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return fieldMap[propertyName];
        }

        public void ToObject(Document source, float score, T target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyFromDocument(source, score, target);
            }
        }

        public void ToDocument(T source, Document target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyToDocument(source, target);
            }
        }

        public DocumentKey ToKey(T source)
        {
            var values = keyFields.ToDictionary(f => (IFieldMappingInfo)f, f => f.PropertyInfo.GetValue(source, null));

            return new DocumentKey(values);
        }

        public IEnumerable<string> AllFields
        {
            get { return fieldMap.Values.Select(m => m.FieldName); }
        }

        public bool EnableScoreTracking
        {
            get { return fieldMap.Values.Any(m => m is ReflectionScoreMapper<T>); }
        }

        public List<IFieldMapper<T>> KeyFields
        {
            get { return new List<IFieldMapper<T>>(keyFields); }
        }

        public bool Equals(T item1, T item2)
        {
            foreach (var field in fieldMap.Values)
            {
                var val1 = field.PropertyInfo.GetValue(item1, null);
                var val2 = field.PropertyInfo.GetValue(item2, null);

                if (!ValuesEqual(val1, val2))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ValuesEqual(object val1, object val2)
        {
            if (val1 is IEnumerable && val2 is IEnumerable)
            {
                return ((IEnumerable) val1).Cast<object>().SequenceEqual(((IEnumerable) val2).Cast<object>());
            }

            return Equals(val1, val2);
        }
    }
}