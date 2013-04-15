using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

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

        public string EscapeSpecialCharacters(string value)
        {
            return QueryParser.Escape(value ?? string.Empty);
        }

        public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            throw new NotSupportedException();
        }

        public Query CreateQuery(string ignored)
        {
            return new TermQuery(new Term(FieldName, value));
        }

        public SortField CreateSortField(bool reverse)
        {
            throw new NotSupportedException();
        }

        public string PropertyName
        {
            get { return "**DocumentKey**" + fieldName; }
        }

        public string FieldName
        {
            get { return fieldName; }
        }

        public Analyzer Analyzer
        {
            get { return new KeywordAnalyzer(); }
        }
    }
}
