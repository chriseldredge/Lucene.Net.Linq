using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    internal class ReflectionDocumentMapper<T> : IDocumentMapper<T>
    {
        private readonly IDictionary<string, IFieldMapper<T>> fieldMap = new Dictionary<string, IFieldMapper<T>>();
        private readonly List<IFieldMapper<T>> keyFields = new List<IFieldMapper<T>>();

        public ReflectionDocumentMapper() : this(typeof(T))
        {
        }

        public ReflectionDocumentMapper(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            BuildFieldMap(props);

            BuildKeyFieldMap(type, props);
        }

        private void BuildFieldMap(IEnumerable<PropertyInfo> props)
        {
            foreach (var p in props)
            {
                if (p.GetCustomAttribute<IgnoreFieldAttribute>(true) != null)
                {
                    continue;
                }
                var mappingContext = FieldMappingInfoBuilder.Build<T>(p);
                fieldMap.Add(mappingContext.PropertyName, mappingContext);
            }
        }

        private void BuildKeyFieldMap(Type type, IEnumerable<PropertyInfo> props)
        {
            var keyProps = from p in props
                           let a = p.GetCustomAttribute<BaseFieldAttribute>(true)
                           where a != null && a.Key
                           select p;

            keyFields.AddRange(keyProps.Select(kp => fieldMap[kp.Name]));

            foreach (var attr in type.GetCustomAttributes<DocumentKeyAttribute>(true))
            {
                var keyField = new DocumentKeyFieldMapper<T>(attr.FieldName, attr.Value);
                fieldMap.Add(keyField.PropertyName, keyField);
                keyFields.Add(keyField);
            }
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

        public IDocumentKey ToKey(T source)
        {
            var keyValues = keyFields.ToDictionary(f => (IFieldMappingInfo)f, f => f.GetPropertyValue(source));

            Validate(keyValues);

            return new DocumentKey(keyValues);
        }

        private void Validate(Dictionary<IFieldMappingInfo, object> keyValues)
        {
            var nulls = keyValues.Where(kv => kv.Value == null).ToArray();

            if (!nulls.Any()) return;

            var message = string.Format("Cannot create key for document of type '{0}' with null value(s) for properties {1} which are marked as Key=true.",
                typeof(T),
                string.Join(", ", nulls.Select(n => n.Key.PropertyName)));

            throw new InvalidOperationException(message);
        }

        public IEnumerable<string> AllFields
        {
            get { return fieldMap.Values.Select(m => m.FieldName); }
        }

        public bool EnableScoreTracking
        {
            get { return fieldMap.Values.Any(m => m is ReflectionScoreMapper<T>); }
        }

        public IEnumerable<string> KeyProperties
        {
            get { return keyFields.Select(k => k.PropertyName); }
        }

        public List<IFieldMapper<T>> KeyFields
        {
            get { return new List<IFieldMapper<T>>(keyFields); }
        }

        public bool Equals(T item1, T item2)
        {
            foreach (var field in fieldMap.Values)
            {
                var val1 = field.GetPropertyValue(item1);
                var val2 = field.GetPropertyValue(item2);

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