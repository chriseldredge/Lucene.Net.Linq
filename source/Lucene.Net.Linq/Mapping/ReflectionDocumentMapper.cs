using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;
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
    public class ReflectionDocumentMapper<T> : DocumentMapperBase<T>
    {
        /// <summary>
        /// Constructs an instance that will create an <see cref="Analyzer"/>
        /// using metadata on public properties on the type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="version">Version compatibility for analyzers and indexers.</param>
        public ReflectionDocumentMapper(Version version)
            : this(version, null)
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
            : base(version, externalAnalyzer)
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
				
				var computedField = p.GetCustomAttribute<ComputedFieldAttribute>(true);
				if (computedField != null)
				{
					AddField(new ComputedFieldMapper<T>(p, computedField.FieldComputerInstance));
					continue;
				}

				AddField(FieldMappingInfoBuilder.Build<T>(p, version, externalAnalyzer));
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
                AddKeyField(new DocumentKeyFieldMapper<T>(attr.FieldName, attr.Value));
            }
        }
    }
}