﻿using System;
using System.ComponentModel;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
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
            : this(null)
        {
        }

        protected BaseFieldAttribute(string field)
        {
            this.field = field;
            Store = StoreMode.Yes;
            Boost = 1.0f;
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
        public Type Converter
        {
            get { return ConverterInstance != null ? ConverterInstance.GetType() : null; }
            set { ConverterInstance = (TypeConverter) Activator.CreateInstance(value); }
        }

        /// <summary>
        /// Specifies that the property value, combined with any other properties that also
        /// specify <code>Key = true</code>, represents a unique primary key to the document.
        ///
        /// Key fields are used to replace or delete documents.
        /// </summary>
        public bool Key { get; set; }

        /// <summary>
        /// Specifies a boost to apply when a document is being analyzed during indexing.
        /// Defaults to <c>1.0f</c>.
        /// </summary>
        public float Boost { get; set; }

        internal TypeConverter ConverterInstance { get; set; }
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
            : this(IndexMode.Analyzed)
        {
        }

        /// <param name="indexMode">How the field should be indexed for searching and sorting.</param>
        public FieldAttribute(IndexMode indexMode)
            : this(null, indexMode)
        {
        }

        /// <param name="field">Backing field used to store data in Lucene index.</param>
        public FieldAttribute(string field)
            : this(field, IndexMode.Analyzed)
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

        /// <summary>
        /// When <c>true</c>, causes <c cref="QueryParser.LowercaseExpandedTerms"/> to
        /// be set to false to prevent wildcard queries like <c>Foo*</c> from being
        /// converted to lowercase.
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets the default parser operator.
        /// </summary>
        /// <value>
        /// The default parser operator.
        /// </value>
        public QueryParser.Operator DefaultParserOperator { get; set; }

        /// <summary>
        /// When set, supplies a custom analyzer for this field. The analyzer type
        /// must have either a parameterless public constructor, or a public constructor
        /// that accepts a <see cref="Net.Util.Version"/> argument.
        ///
        /// When an external Analyzer is provided on <see cref="LuceneDataProvider"/>
        /// methods it will override this setting.
        /// </summary>
        public Type Analyzer { get; set; }

        /// <summary>
        /// Maps to <see cref="Field.TermVector"/>
        /// </summary>
        public TermVectorMode TermVector { get; set; }

        /// <summary>
        /// When <c>true</c> and the property implements <see cref="IComparable"/>
        /// and/or <see cref="IComparable{T}"/>, instructs the mapping engine to
        /// use <see cref="SortField.STRING"/> instead of converting each field
        /// and using <see cref="IComparable{T}.CompareTo"/>. This is a performance
        /// enhancement in cases where the string representation of a complex type
        /// is alphanumerically sortable.
        /// </summary>
        public bool NativeSort { get; set; }
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

    /// <summary>
    /// When set on a property, the document boost will be set with the property
    /// value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DocumentBoostAttribute : Attribute
    {
    }

    /// <summary>
    /// When set on a class, defines a fixed-value key that will always
    /// be used when querying for objects of this type or deleting and
    /// replacing documents with matching keys.
    ///
    /// This attribute enables multiple object types to be stored in
    /// the same index by ensuring that unrelated documents of other
    /// types will not be returned when querying.
    /// </summary>
    /// <example>
    /// <code>
    ///   [DocumentKey(FieldName="Type", Value="Customer")]
    ///   public class Customer
    ///   {
    ///   }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DocumentKeyAttribute : Attribute
    {
        /// <summary>
        /// The field name that will be queried.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The constant value that will be queried.
        /// </summary>
        public string Value { get; set; }
    }
}
