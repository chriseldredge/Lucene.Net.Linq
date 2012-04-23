using System;
using Lucene.Net.Util;

namespace Lucene.Net.Linq.Mapping
{
    public abstract class BaseFieldAttribute : Attribute
    {
        private readonly string field;

        protected BaseFieldAttribute()
            :this(null)
        {
        }

        /// <param name="field">Backing field used to store data in Lucene index.</param>
        protected BaseFieldAttribute(string field)
        {
            this.field = field;
            Store = true;
        }

        public string Field { get { return field; } }

        /// <summary>
        /// Set to true to store value in index for later retrieval, or
        /// false if the field should only be indexed.
        /// </summary>
        /// TODO: enable compression
        public bool Store { get; set; }
        public Type Converter { get; set; }
    }

    public enum IndexMode
    {
        Analyzed,
        AnalyzedNoNorms,
        NotAnalyzed,
        NotAnalyzedNoNorms,
        NotIndexed
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FieldAttribute : BaseFieldAttribute
    {
        private readonly IndexMode indexMode;

        public FieldAttribute()
        {
        }

        public FieldAttribute(IndexMode indexMode)
            : this(null, indexMode)
        {
        }

        public FieldAttribute(string field)
            : base(field)
        {
        }

        public FieldAttribute(string field, IndexMode indexMode)
            : base(field)
        {
            this.indexMode = indexMode;
        }

        
        public IndexMode IndexMode { get { return indexMode; } }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NumericFieldAttribute : BaseFieldAttribute
    {
        public NumericFieldAttribute()
            : this(null)
        {
        }

        public NumericFieldAttribute(string field)
            : base(field)
        {
            PrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        }

        public int PrecisionStep { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IgnoreFieldAttribute : Attribute
    {
    }
}
