using System;
using System.ComponentModel;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Holds mapping information that allows
    /// properties on types to be mapped to Lucene
    /// Fields and vice versa.
    /// </summary>
    public interface IFieldMappingInfo
    {
        /// <summary>
        /// Name of Lucene field. By default, this
        /// will be the same as <see cref="PropertyName"/>.
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// Property name.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// Property type.
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// Type converter used to convert complex
        /// types to strings. Will be <c>null</c>
        /// for primitive types and strings.
        /// </summary>
        TypeConverter Converter { get; }
        
        /// <summary>
        /// Flag indicating if the field
        /// should be stored as a <see cref="NumericField"/>
        /// encoded using a binary trie structure.
        /// </summary>
        bool IsNumericField { get; }

        /// <summary>
        /// Specifies how sorting on the field should
        /// be done. See <see cref="SortField"/> for
        /// appropriate values.
        /// </summary>
        int SortFieldType { get; }

        /// <summary>
        /// Flag indicating if the field is case sensitive,
        /// in which case <see cref="QueryParser.LowercaseExpandedTerms"/>
        /// will be disabled when querying on this field.
        /// </summary>
        bool CaseSensitive { get; }

        /// <summary>
        /// Specifies a constraint that is used to limit
        /// queries to documents that match the constraint.
        /// Used in conjunction with <see cref="DocumentKeyAttribute"/>
        /// and <see cref="BaseFieldAttribute.Key"/>.
        /// 
        /// Fields that are not keys may return null.
        /// </summary>
        Query KeyConstraint { get; }

        /// <summary>
        /// In cases of complex types or numeric fields,
        /// converts a value into a query expression.
        /// For string fields, simply returns a string
        /// representation of the value.
        /// </summary>
        string ConvertToQueryExpression(object value);
    }
}