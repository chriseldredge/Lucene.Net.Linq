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

        public NumericReflectionFieldMapper(PropertyInfo propertyInfo, StoreMode store, TypeConverter typeToValueTypeConverter, TypeConverter valueTypeToStringConverter, string field, int precisionStep)
            : base(propertyInfo, store, IndexMode.Analyzed, valueTypeToStringConverter, field, false, new KeywordAnalyzer())
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

        protected internal override object ConvertFieldValue(Field field)
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
            var value = propertyInfo.GetValue(source, null);

            target.RemoveFields(fieldName);

            if (value == null) return;

            value = ConvertToSupportedValueType(value);

            var numericField = new NumericField(fieldName, precisionStep, FieldStore, true);

            numericField.SetValue((ValueType)value);

            target.Add(numericField);
        }

        public override string ConvertToQueryExpression(object value)
        {
            value = ConvertToSupportedValueType(value);
            
            return ((ValueType) value).ToPrefixCoded();
        }

        public override Query CreateQuery(string pattern)
        {
            return new TermQuery(new Term(FieldName, pattern));
        }

        public override Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            return NumericRangeUtils.CreateNumericRangeQuery(fieldName, (ValueType)lowerBound, (ValueType)upperBound, lowerRange, upperRange);
        }

        private object ConvertToSupportedValueType(object value)
        {
            if (typeToValueTypeConverter == null) return value;

            var type = GetUnderlyingValueType();

            return type != null ? typeToValueTypeConverter.ConvertTo(value, type) : value;
        }

        private Type GetUnderlyingValueType()
        {
            return supportedValueTypes.FirstOrDefault(t => typeToValueTypeConverter.CanConvertTo(t));
        }
    }
}