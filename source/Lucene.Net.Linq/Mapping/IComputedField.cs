using Lucene.Net.Documents;
using Lucene.Net.Linq.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
	/// <summary>
	/// Represents a field that will be computed based on other fields within a document.
	/// </summary>
	public interface IComputedField
	{
		/// <summary>
		/// Retrieve a field from the given document and
		/// convert it to a value suitable for the given mapping.
		/// </summary>
		object GetFieldValue(Document document);

		/// <summary>
		/// Creates a query based on the supplied pattern.
		/// The pattern should be analyzed and parsed
		/// (typically by using a <see cref="QueryParser"/>)
		/// to analyze the pattern and create
		/// <see cref="WildcardQuery"/>, <see cref="PhraseQuery"/>
		/// or <see cref="TermQuery"/> as needed.
		/// </summary>
		Query CreateQuery(string pattern);

		/// <summary>
		/// Creates a range query with the provided criteria.
		/// </summary>
		Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange);

		/// <summary>
		/// In cases of complex types or numeric fields,
		/// converts a value into a query expression.
		/// For string fields, simply returns a string
		/// representation of the value.
		/// </summary>
		string ConvertToQueryExpression(object value);
	}
}