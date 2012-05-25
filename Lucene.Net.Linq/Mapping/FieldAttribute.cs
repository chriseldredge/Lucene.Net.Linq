using System;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Attribute = System.Attribute;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Base attribute for customizing how properties are stored and indexed.
    /// </summary>
    public abstract class BaseFieldAttribute : Attribute
    {
        private readonly string field;

        protected BaseFieldAttribute()
            :this(null)
        {
        }

        protected BaseFieldAttribute(string field)
        {
            this.field = field;
            Store = StoreMode.Yes;
        }

        /// <summary>
        /// Specifies the name of the backing field that the property value will be mapped to.
        /// When not specified, defaults to the name of the property being decorated by this attribute.
        /// </summary>
        public string Field { get { return field; } }

        /// <summary>
        /// Set to true to store value in index for later retrieval, or
        /// false if the field should only be indexed.
        /// </summary>
        public StoreMode Store { get; set; }

        /// <summary>
        /// Provides a custom TypeConverter implementation that can convert the property type
        /// to and from strings so they can be stored and indexed by Lucene.Net.
        /// </summary>
        public Type Converter { get; set; }
    }

    /// <summary>
    /// Customizes how a property is converted to a field as well as
    /// storage and indexing options.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FieldAttribute : BaseFieldAttribute
    {
        private readonly IndexMode indexMode;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FieldAttribute()
        {
        }

        /// <param name="indexMode">How the field should be indexed for searching and sorting.</param>
        public FieldAttribute(IndexMode indexMode)
            : this(null, indexMode)
        {
        }

        /// <param name="field">Backing field used to store data in Lucene index.</param>
        public FieldAttribute(string field)
            : base(field)
        {
        }

        /// <param name="field">Backing field used to store data in Lucene index.</param>
        /// <param name="indexMode">How the field should be indexed for searching and sorting.</param>
        public FieldAttribute(string field, IndexMode indexMode)
            : base(field)
        {
            this.indexMode = indexMode;
        }

        /// <summary>
        /// How the field should be indexed for searching and sorting.
        /// </summary>
        public IndexMode IndexMode { get { return indexMode; } }

        /// <summary>
        /// Overrides default format pattern to use when converting ValueType
        /// to string. If both <c cref="Format">Format</c> and
        /// <c cref="BaseFieldAttribute.Converter">Converter</c> are specified, <c>Converter</c>
        /// will take precedence and <c>Format</c> will be ignored.
        /// </summary>
        public string Format { get; set; }
    }

    /// <summary>
    /// Maps a <c cref="ValueType"/>, or any type that can be converted
    /// to <c cref="int"/>, <c cref="long"/>, <c cref="double"/>, or
    /// <c cref="float"/> to a <c cref="NumericField"/> that will be
    /// indexed as a trie structure to enable more efficient range filtering
    /// and sorting.
    /// </summary>
    /// <see cref="NumericField"/>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NumericFieldAttribute : BaseFieldAttribute
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NumericFieldAttribute()
            : this(null)
        {
        }

        /// <param name="field">Backing field used to store data in Lucene index.</param>
        public NumericFieldAttribute(string field)
            : base(field)
        {
            PrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        }

        /// <see cref="NumericRangeQuery"/> 
        public int PrecisionStep { get; set; }
    }

    /// <summary>
    /// Specifies that a public property should be ignored by the Lucene.Net.Linq
    /// mapping engine when converting objects to Documents and vice-versa.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreFieldAttribute : Attribute
    {
    }

    /// <summary>
    /// When set on a property, the property will be set with the score (relevance)
    /// of the document based on the queries and boost settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class QueryScoreAttribute : Attribute
    {
    }
}
