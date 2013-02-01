using System;
using System.ComponentModel;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    internal class DocumentKeyFieldMapper<T> : IFieldMapper<T>
    {
        private readonly string fieldName;
        private readonly string value;

        public DocumentKeyFieldMapper(string fieldName, string value)
        {
            this.fieldName = fieldName;
            this.value = value;
        }

        public Query KeyConstraint
        {
            get { return new TermQuery(new Term(FieldName, value)); }
        }

        public object GetPropertyValue(T source)
        {
            return value;
        }

        public void CopyToDocument(T source, Document target)
        {
            target.Add(new Field(fieldName, value, Field.Store.YES, Field.Index.NOT_ANALYZED));
        }

        public void CopyFromDocument(Document source, IQueryExecutionContext context, T target)
        {
        }

        public string ConvertToQueryExpression(object value)
        {
            return this.value;
        }

        public bool CaseSensitive
        {
            get { return true; }
        }

        public int SortFieldType
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsNumericField
        {
            get { return false; }
        }

        public TypeConverter Converter
        {
            get { return null; }
        }

        public Type PropertyType
        {
            get { return typeof (string); }
        }

        public string PropertyName
        {
            get { return "**DocumentKey**" + fieldName; }
        }

        public string FieldName
        {
            get { return fieldName; }
        }
    }
}