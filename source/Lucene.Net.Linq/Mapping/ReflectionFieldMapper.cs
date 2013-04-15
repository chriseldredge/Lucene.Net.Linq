using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Mapping
{
    public class ReflectionFieldMapper<T> : IFieldMapper<T>
    {
        protected readonly PropertyInfo propertyInfo;
        protected readonly StoreMode store;
        protected readonly IndexMode index;
        protected readonly TermVectorMode termVector;
        protected readonly TypeConverter converter;
        protected readonly string fieldName;
        protected readonly bool caseSensitive;
        protected readonly Analyzer analyzer;
        protected readonly float boost;

        public ReflectionFieldMapper(PropertyInfo propertyInfo, StoreMode store, IndexMode index, TermVectorMode termVector,
                                     TypeConverter converter, string fieldName, bool caseSensitive, Analyzer analyzer)
            : this(propertyInfo, store, index, termVector, converter, fieldName, caseSensitive, analyzer, 1f)
        {
            
        }

        public ReflectionFieldMapper(PropertyInfo propertyInfo, StoreMode store, IndexMode index, TermVectorMode termVector, TypeConverter converter, string fieldName, bool caseSensitive, Analyzer analyzer, float boost)
        {
            this.propertyInfo = propertyInfo;
            this.store = store;
            this.index = index;
            this.termVector = termVector;
            this.converter = converter;
            this.fieldName = fieldName;
            this.caseSensitive = caseSensitive;
            this.analyzer = analyzer;
            this.boost = boost;
        }

        public virtual Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public virtual PropertyInfo PropertyInfo
        {
            get { return propertyInfo; }
        }

        public virtual StoreMode Store
        {
            get { return store; }
        }

        public virtual IndexMode IndexMode
        {
            get { return index; }
        }

        public virtual TermVectorMode TermVector
        {
            get { return termVector; }
        }

        public virtual TypeConverter Converter
        {
            get { return converter; }
        }

        public virtual string FieldName
        {
            get { return fieldName; }
        }

        public virtual bool CaseSensitive
        {
            get { return caseSensitive; }
        }

        public virtual float Boost
        {
            get { return boost; }
        }

        public virtual string PropertyName { get { return propertyInfo.Name; } }
        
        public virtual object GetPropertyValue(T source)
        {
            return propertyInfo.GetValue(source, null);
        }

        public virtual void CopyFromDocument(Document source, IQueryExecutionContext context, T target)
        {
            var field = source.GetFieldable(fieldName);

            if (field == null) return;
            
            if (!propertyInfo.CanWrite) return;

            var fieldValue = ConvertFieldValue(field);

            propertyInfo.SetValue(target, fieldValue, null);
        }

        public virtual void CopyToDocument(T source, Document target)
        {
            var value = propertyInfo.GetValue(source, null);

            target.RemoveFields(fieldName);

            AddField(target, value);
        }

        public virtual string ConvertToQueryExpression(object value)
        {
            if (converter != null)
            {
                return (string)converter.ConvertTo(value, typeof(string));
            }

            return (string)value;
        }

        public virtual string EscapeSpecialCharacters(string value)
        {
            return QueryParser.Escape(value ?? string.Empty);
        }

        public virtual Query CreateQuery(string pattern)
        {
            var queryParser = new QueryParser(Version.LUCENE_30, FieldName, analyzer)
                {
                    AllowLeadingWildcard = true,
                    LowercaseExpandedTerms = !CaseSensitive
                };

            return queryParser.Parse(pattern);
        }

        public virtual Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            var minInclusive = lowerRange == RangeType.Inclusive;
            var maxInclusive = upperRange == RangeType.Inclusive;

            var lowerBoundStr = lowerBound == null ? null : EvaluateExpressionToStringAndAnalyze(lowerBound);
            var upperBoundStr = upperBound == null ? null : EvaluateExpressionToStringAndAnalyze(upperBound);
            return new TermRangeQuery(FieldName, lowerBoundStr, upperBoundStr, minInclusive, maxInclusive);
        }

        public virtual SortField CreateSortField(bool reverse)
        {
            if (Converter == null) return new SortField(FieldName, SortField.STRING, reverse);

            var propertyType = propertyInfo.PropertyType;

            FieldComparatorSource source;

            if (typeof(IComparable).IsAssignableFrom(propertyType))
            {
                source = new NonGenericConvertableFieldComparatorSource(propertyType, Converter);
            }
            else if (typeof(IComparable<>).MakeGenericType(propertyType).IsAssignableFrom(propertyType))
            {
                source = new GenericConvertableFieldComparatorSource(propertyType, Converter);
            }
            else
            {
                throw new NotSupportedException("Unsupported sort field type (does not implement IComparable): " +
                                                propertyType);
            }

            return new SortField(FieldName, source, reverse);
        }

        private string EvaluateExpressionToStringAndAnalyze(object value)
        {
            return analyzer.Analyze(FieldName, ConvertToQueryExpression(value));
        }

        protected internal virtual object ConvertFieldValue(IFieldable field)
        {
            var fieldValue = (object)field.StringValue;

            if (converter != null)
            {
                fieldValue = converter.ConvertFrom(fieldValue);
            }
            return fieldValue;
        }

        protected internal void AddField(Document target, object value)
        {
            if (value == null) return;

            var fieldValue = (string)null;

            if (converter != null)
            {
                fieldValue = (string)converter.ConvertTo(value, typeof(string));
            }
            else if (value is string)
            {
                fieldValue = (string)value;
            }

            if (fieldValue != null)
            {
                var field = new Field(fieldName, fieldValue, FieldStore, (Field.Index) index, (Field.TermVector) TermVector);
                field.Boost = Boost;
                target.Add(field);
            }
        }

        protected Field.Store FieldStore
        {
            get
            {
                return (Field.Store) store;
            }
        }
    }
}
