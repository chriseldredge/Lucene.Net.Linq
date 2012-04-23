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
}