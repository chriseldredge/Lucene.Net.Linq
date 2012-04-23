using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.Mapping
{
    public interface IFieldMapper<in T> : IFieldMappingInfo
    {
        void CopyFromDocument(Document source, T target);
        void CopyToDocument(T source, Document target);
    }

    public interface IFieldMappingInfo
    {
        string FieldName { get; }
        TypeConverter Converter { get; }
        PropertyInfo PropertyInfo { get; }
        bool IsNumericField { get; }
        string ConvertToQueryExpression(object value);
    }

    public class ReflectionFieldMapper<T> : IFieldMapper<T>
    {
        protected readonly PropertyInfo propertyInfo;
        protected readonly bool store;
        protected readonly IndexMode indexMode;
        protected readonly TypeConverter converter;
        protected readonly string fieldName;

        public ReflectionFieldMapper(PropertyInfo propertyInfo, bool store, IndexMode indexMode, TypeConverter converter, string fieldName)
        {
            this.propertyInfo = propertyInfo;
            this.store = store;
            this.indexMode = indexMode;
            this.converter = converter;
            this.fieldName = fieldName;
        }

        public PropertyInfo PropertyInfo
        {
            get { return propertyInfo; }
        }

        public bool Store
        {
            get { return store; }
        }

        public IndexMode IndexMode
        {
            get { return indexMode; }
        }

        public TypeConverter Converter
        {
            get { return converter; }
        }

        public string FieldName
        {
            get { return fieldName; }
        }

        public virtual bool IsNumericField { get { return false; } }

        public virtual void CopyFromDocument(Document source, T target)
        {
            var field = source.GetField(fieldName);

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

        protected internal virtual object ConvertFieldValue(Field field)
        {
            var fieldValue = (object)field.StringValue();

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
            else if (value.GetType() == typeof(string))
            {
                fieldValue = (string)value;
            }

            if (fieldValue != null)
            {
                target.Add(new Field(fieldName, fieldValue, FieldStore, indexMode.ToFieldIndex()));
            }
        }

        protected Field.Store FieldStore
        {
            get { return store ? Field.Store.YES : Field.Store.NO; }
        }
    }

    public class NumericReflectionFieldMapper<T> : ReflectionFieldMapper<T>
    {
        private readonly TypeConverter typeToValueTypeConverter;
        private readonly int precisionStep;

        public NumericReflectionFieldMapper(PropertyInfo propertyInfo, bool store, TypeConverter typeToValueTypeConverter, TypeConverter valueTypeToStringConverter, string field, int precisionStep)
            : base(propertyInfo, store, IndexMode.Analyzed, valueTypeToStringConverter, field)
        {
            this.typeToValueTypeConverter = typeToValueTypeConverter;
            this.precisionStep = precisionStep;
        }

        public int PrecisionStep
        {
            get { return precisionStep; }
        }

        public override bool IsNumericField
        {
            get { return true; }
        }

        protected internal override object ConvertFieldValue(Field field)
        {
            var type = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            // TODO: move this
            if (type == typeof(DateTimeOffset))
            {
                return new DateTimeOffset(long.Parse(field.StringValue()), TimeSpan.Zero);
            }

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

        private object ConvertToSupportedValueType(object value)
        {
            if (typeToValueTypeConverter == null) return value;

            if (typeToValueTypeConverter.CanConvertTo(typeof(long)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(long));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(int)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(int));
            }
            if (converter.CanConvertTo(typeof(double)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(double));
            }
            if (typeToValueTypeConverter.CanConvertTo(typeof(float)))
            {
                return typeToValueTypeConverter.ConvertTo(value, typeof(float));
            }

            return value;
        }
    }

    public class CollectionReflectionFieldMapper<T> : ReflectionFieldMapper<T>
    {
        private readonly Type elementType;

        public CollectionReflectionFieldMapper(ReflectionFieldMapper<T> inner, Type elementType)
            : base(inner.PropertyInfo, inner.Store, inner.IndexMode, inner.Converter, inner.FieldName)
        {
            this.elementType = elementType;
        }

        public override void CopyFromDocument(Document source, T target)
        {
            var values = new ArrayList();

            foreach(var value in source.GetFields(fieldName))
            {
                values.Add(ConvertFieldValue(value));
            }

            propertyInfo.SetValue(target, values.ToArray(elementType), null);
        }

        public override void CopyToDocument(T source, Document target)
        {
            target.RemoveFields(fieldName);

            var value = (IEnumerable)PropertyInfo.GetValue(source, null);

            if (value == null)
            {
                return;
            }

            foreach (var item in value)
            {
                AddField(target, item);
            }
        }
    }
}