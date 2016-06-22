using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    internal class NumericReflectionFieldMapper<T> : ReflectionFieldMapper<T>
    {
        private static readonly IEnumerable<Type> supportedValueTypes = new List<Type>{typeof(long), typeof(int), typeof(double), typeof(float)};

        private readonly TypeConverter typeToValueTypeConverter;
        private readonly int precisionStep;

        public NumericReflectionFieldMapper(PropertyInfo propertyInfo, StoreMode store, TypeConverter typeToValueTypeConverter, TypeConverter valueTypeToStringConverter, string field, int precisionStep, float boost)
            : base(propertyInfo, store, IndexMode.Analyzed, TermVectorMode.No, valueTypeToStringConverter, field, false, new KeywordAnalyzer(), boost)
        {
            this.typeToValueTypeConverter = typeToValueTypeConverter;
            this.precisionStep = precisionStep;
        }

        public int PrecisionStep
        {
            get { return precisionStep; }
        }

        public override SortField CreateSortField(bool reverse)
        {
            var targetType = propertyInfo.PropertyType;

            if (typeToValueTypeConverter != null)
            {
                targetType = GetUnderlyingValueType();
            }

            return new SortField(FieldName, targetType.ToSortField(), reverse);
        }

        protected internal override object ConvertFieldValue(IFieldable field)
        {
            var value = base.ConvertFieldValue(field);

            if (typeToValueTypeConverter != null)
            {
                value = typeToValueTypeConverter.ConvertFrom(value);
            }

            return value;
        }

        public override void CopyToDocument(T source, Document target)
        {
            var value = propertyGetter(source);

            target.RemoveFields(fieldName);

            if (value == null) return;

            value = ConvertToSupportedValueType(value);

            var numericField = new NumericField(fieldName, precisionStep, FieldStore, true);
            numericField.SetValue((ValueType)value);

            SetBoostIfNotDefault(numericField);

            target.Add(numericField);
        }

        private void SetBoostIfNotDefault(NumericField numericField)
        {
            const float threshold = 0.002f;
            var diff = Math.Abs(Boost - 1.0f);
            
            if (diff < threshold) return;

            numericField.ForceDisableOmitNorms();
            numericField.Boost = Boost;
        }

        public override string ConvertToQueryExpression(object value)
        {
            value = ConvertToSupportedValueType(value);

            return ((ValueType) value).ToPrefixCoded();
        }

        public override string EscapeSpecialCharacters(string value)
        {
            // no need to escape since value will not be parsed.
            return value;
        }

        public override Query CreateQuery(string pattern)
        {
            if (pattern == "*")
            {
                return base.CreateQuery(pattern);
            }
            
            return new TermQuery(new Term(FieldName, pattern));
        }

        public override Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            if (lowerBound != null && !propertyInfo.PropertyType.IsInstanceOfType(lowerBound))
            {
                lowerBound = ConvertToSupportedValueType(lowerBound);
            }
            if (upperBound != null && !propertyInfo.PropertyType.IsInstanceOfType(upperBound))
            {
                upperBound = ConvertToSupportedValueType(upperBound);
            }
            return NumericRangeUtils.CreateNumericRangeQuery(fieldName, (ValueType)lowerBound, (ValueType)upperBound, lowerRange, upperRange);
        }

        private object ConvertToSupportedValueType(object value)
        {
            if (value is string && (string) value == "*") return null;

            var propertyType = propertyInfo.PropertyType.GetUnderlyingType();

            if (typeToValueTypeConverter == null)
            {
                return Convert.ChangeType(value, propertyType);
            }

            var type = GetUnderlyingValueType();

            if (!typeToValueTypeConverter.CanConvertFrom(null, value.GetType()))
            {
                value = Convert.ChangeType(value, propertyType);
            }

            return type != null ? typeToValueTypeConverter.ConvertTo(value, type) : value;
        }

        private Type GetUnderlyingValueType()
        {
            return supportedValueTypes.FirstOrDefault(t => typeToValueTypeConverter.CanConvertTo(t));
        }
    }
}
