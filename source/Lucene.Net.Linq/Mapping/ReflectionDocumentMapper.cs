using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Linq.Util;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Maps public properties on <typeparamref name="T"/> to
    /// Lucene <see cref="Field"/>s using optional metadata
    /// attributes such as <see cref="FieldAttribute"/>,
    /// <see cref="NumericFieldAttribute"/>,
    /// <see cref="IgnoreFieldAttribute"/>,
    /// <see cref="DocumentKeyAttribute"/>
    /// and <see cref="QueryScoreAttribute"/>.
    /// </summary>
    public class ReflectionDocumentMapper<T> : IDocumentMapper<T>
    {
        protected readonly Analyzer externalAnalyzer;
        protected readonly PerFieldAnalyzer analyzer;
        protected readonly Version version;
        protected readonly IDictionary<string, IFieldMapper<T>> fieldMap = new Dictionary<string, IFieldMapper<T>>();
        protected readonly List<IFieldMapper<T>> keyFields = new List<IFieldMapper<T>>();

        /// <summary>
        /// Constructs an instance that will create an <see cref="Analyzer"/>
        /// using metadata on public properties on the type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="version">Version compatibility for analyzers and indexers.</param>
        public ReflectionDocumentMapper(Version version)
            : this(version, null, typeof(T))
        {
        }

        /// <summary>
        /// Constructs an instance with an externall supplied analyzer
        /// and the compatibility version of the index.
        /// </summary>
        /// <param name="version">Version compatibility for analyzers and indexers.</param>
        /// <param name="externalAnalyzer"></param>
        public ReflectionDocumentMapper(Version version, Analyzer externalAnalyzer)
            : this(version, externalAnalyzer, typeof(T))
        {
        }

        private ReflectionDocumentMapper(Version version, Analyzer externalAnalyzer, Type type)
        {
            this.externalAnalyzer = externalAnalyzer;
            this.version = version;
            this.analyzer = new PerFieldAnalyzer(new KeywordAnalyzer());

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
                var mappingContext = FieldMappingInfoBuilder.Build<T>(p, version, externalAnalyzer);
                
                fieldMap.Add(mappingContext.PropertyName, mappingContext);

                if (!string.IsNullOrWhiteSpace(mappingContext.FieldName) && mappingContext.Analyzer != null)
                {
                    analyzer.AddAnalyzer(mappingContext.FieldName, mappingContext.Analyzer);
                }
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

        public virtual PerFieldAnalyzer Analyzer
        {
            get { return analyzer; }
        }

        public virtual IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return fieldMap[propertyName];
        }

        public virtual void ToObject(Document source, IQueryExecutionContext context, T target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyFromDocument(source, context, target);
            }
        }

        public virtual void ToDocument(T source, Document target)
        {
            foreach (var mapping in fieldMap)
            {
                mapping.Value.CopyToDocument(source, target);
            }
        }

        public virtual IDocumentKey ToKey(T source)
        {
            var keyValues = keyFields.ToDictionary(f => (IFieldMappingInfo)f, f => f.GetPropertyValue(source));

            ValidateKey(keyValues);

            return new DocumentKey(keyValues);
        }

        private void ValidateKey(Dictionary<IFieldMappingInfo, object> keyValues)
        {
            var nulls = keyValues.Where(kv => kv.Value == null).ToArray();

            if (!nulls.Any()) return;

            var message = string.Format("Cannot create key for document of type '{0}' with null value(s) for properties {1} which are marked as Key=true.",
                typeof(T),
                string.Join(", ", nulls.Select(n => n.Key.PropertyName)));

            throw new InvalidOperationException(message);
        }

        public virtual IEnumerable<string> AllProperties
        {
            get { return fieldMap.Values.Select(m => m.PropertyName); }
        }

        public virtual void PrepareSearchSettings(IQueryExecutionContext context)
        {
            if (EnableScoreTracking)
            {
                context.Searcher.SetDefaultFieldSortScoring(true, false);    
            }
        }

        public Query CreateMultiFieldQuery(string pattern)
        {
            // TODO: pattern should be analyzed/converted on per-field basis.
            var parser = new MultiFieldQueryParser(version, fieldMap.Keys.ToArray(), externalAnalyzer);
            return parser.Parse(pattern);
        }

        public virtual IEnumerable<string> KeyProperties
        {
            get { return keyFields.Select(k => k.PropertyName); }
        }

        public virtual bool Equals(T item1, T item2)
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

        protected internal virtual bool ValuesEqual(object val1, object val2)
        {
            if (val1 is IEnumerable && val2 is IEnumerable)
            {
                return ((IEnumerable) val1).Cast<object>().SequenceEqual(((IEnumerable) val2).Cast<object>());
            }

            return Equals(val1, val2);
        }

        protected virtual bool EnableScoreTracking
        {
            get { return fieldMap.Values.Any(m => m is ReflectionScoreMapper<T>); }
        }
    }
}