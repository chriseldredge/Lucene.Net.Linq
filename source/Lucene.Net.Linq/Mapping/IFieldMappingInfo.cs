using Lucene.Net.Linq.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Mapping
{
    /// <summary>
    /// Holds mapping information that allows
    /// properties on types to be mapped to Lucene
    /// Fields and vice versa.
    /// </summary>
    public interface IFieldMappingInfo
    {
        /// <summary>
        /// Name of Lucene field. By default, this
        /// will be the same as <see cref="PropertyName"/>.
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// Property name.
        /// </summary>
        string PropertyName { get; }
        
        /// <summary>
        /// In cases of complex types or numeric fields,
        /// converts a value into a query expression.
        /// For string fields, simply returns a string
        /// representation of the value.
        /// </summary>
        string ConvertToQueryExpression(object value);

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
        /// Creates an appropriate SortField instance for the
        /// underlying Lucene field.
        /// </summary>
        /// <param name="reverse"></param>
        SortField CreateSortField(bool reverse);
    }
}