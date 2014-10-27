using System;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    internal class ReflectionDocumentBoostMapper<T> : IFieldMapper<T>, IDocumentFieldConverter
    {
        private readonly PropertyInfo propertyInfo;

        public ReflectionDocumentBoostMapper(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
        }

        public object GetFieldValue(Document document)
        {
            return document.Boost;
        }

        public void CopyToDocument(T source, Document target)
        {
            target.Boost = (float)GetPropertyValue(source);
        }

        public void CopyFromDocument(Document source, IQueryExecutionContext context, T target)
        {
            var value = GetFieldValue(source);

            propertyInfo.SetValue(target, value, null);
        }

        public SortField CreateSortField(bool reverse)
        {
            throw new NotSupportedException();
        }

        public string ConvertToQueryExpression(object value)
        {
            throw new NotSupportedException();
        }

        public string EscapeSpecialCharacters(string value)
        {
            throw new NotSupportedException();
        }

        public Query CreateQuery(string pattern)
        {
            throw new NotSupportedException();
        }

        public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            throw new NotSupportedException();
        }

        public object GetPropertyValue(T source)
        {
            return propertyInfo.GetValue(source, null);
        }

        public string PropertyName { get { return propertyInfo.Name; } }
        public string FieldName { get { return null; } }
        public Analyzer Analyzer { get { return null; } }
        public IndexMode IndexMode { get { return IndexMode.NotIndexed; } }
    }
}
