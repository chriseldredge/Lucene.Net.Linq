using System;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
	internal class ComputedFieldMapper<T> : IFieldMapper<T>, IDocumentFieldConverter
	{
		private readonly PropertyInfo propertyInfo;
		private readonly IComputedField field;

		public ComputedFieldMapper(PropertyInfo propertyInfo, IComputedField field)
		{
			this.propertyInfo = propertyInfo;
			this.field = field;
		}

		/// <summary>
		/// Name of Lucene field. By default, this
		/// will be the same as <see cref="PropertyName"/>.
		/// </summary>
		public string FieldName
		{
			get { return propertyInfo.Name; }
		}

		/// <summary>
		/// Property name.
		/// </summary>
		public string PropertyName
		{
			get { return propertyInfo.Name; }
		}

		/// <summary>
		/// In cases of complex types or numeric fields,
		/// converts a value into a query expression.
		/// For string fields, simply returns a string
		/// representation of the value.
		/// </summary>
		public string ConvertToQueryExpression(object value)
		{
			return this.field.ConvertToQueryExpression(value);
		}

		/// <summary>
		/// Esapes special characters in a query pattern
		/// such as asterisk (*).
		/// </summary>
		public string EscapeSpecialCharacters(string value)
		{
			return QueryParser.Escape(value ?? string.Empty);
		}

		/// <summary>
		/// Creates a query based on the supplied pattern.
		/// The pattern should be analyzed and parsed
		/// (typically by using a <see cref="QueryParser"/>)
		/// to analyze the pattern and create
		/// <see cref="WildcardQuery"/>, <see cref="PhraseQuery"/>
		/// or <see cref="TermQuery"/> as needed.
		/// </summary>
		public Query CreateQuery(string pattern)
		{
			return this.field.CreateQuery(pattern);
		}

		/// <summary>
		/// Creates a range query with the provided criteria.
		/// </summary>
		public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
		{
			return this.field.CreateRangeQuery(lowerBound, upperBound, lowerRange, upperRange);
		}

		/// <summary>
		/// Creates an appropriate SortField instance for the
		/// underlying Lucene field.
		/// </summary>
		/// <param name="reverse"></param>
		public SortField CreateSortField(bool reverse)
		{
			return this.field.CreateSortField(reverse);
		}

		/// <summary>
		/// Retrieve <see cref="Field"/> or other metadata
		/// from <paramref name="source"/> and <paramref name="context"/>
		/// and apply to <paramref name="target"/>.
		/// </summary>
		public void CopyFromDocument(Document source, IQueryExecutionContext context, T target)
		{
			if (!propertyInfo.CanWrite) return;

			var fieldValue = GetFieldValue(source);

			propertyInfo.SetValue(target, fieldValue, null);
		}

		/// <summary>
		/// Convert a DefaultSearchProperty or other data on an instance
		/// of <paramref name="source"/> into a <see cref="Field"/>
		/// on the <paramref name="target"/>.
		/// </summary>
		public void CopyToDocument(T source, Document target)
		{
			// NoOp
		}

		/// <summary>
		/// Retrieve a value from <paramref name="source"/>
		/// for the purposes of constructing an <see cref="IDocumentKey"/>
		/// or comparing instances of <typeparamref name="T"/>
		/// to detect dirty objects.
		/// </summary>
		public object GetPropertyValue(T source)
		{
			return propertyInfo.GetValue(source, null);
		}

		/// <summary>
		/// Gets the Analyzer to be used for indexing this field
		/// or parsing queries on this field.
		/// </summary>
		public Analyzer Analyzer
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the index mode for the field.
		/// </summary>
		public IndexMode IndexMode
		{
			get { return IndexMode.NotIndexed; }
		}

		/// <summary>
		/// Retrieve a field from the given document and
		/// convert it to a value suitable for the given mapping.
		/// </summary>
		public object GetFieldValue(Document document)
		{
			return this.field.GetFieldValue(document);
		}
	}
}