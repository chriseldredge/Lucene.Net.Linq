using System;
using System.ComponentModel;
using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Tests
{
    public class FakeFieldMappingInfo : IFieldMappingInfo
    {
        public FakeFieldMappingInfo()
        {
            IsNumericField = true;
        }

        public string FieldName { get; set; }
        public TypeConverter Converter { get; set; }
        public bool IsNumericField { get; set; }

        public string PropertyName { get; set; }
        public Type PropertyType { get; set; }

        public string ConvertToQueryExpression(object value)
        {
            return value.ToString();
        }

        public Query CreateQuery(string pattern)
        {
            return new TermQuery(new Term(FieldName, pattern));
        }

        public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
        {
            return new TermRangeQuery(FieldName,
                lowerBound != null ? lowerBound.ToString() : null,
                upperBound != null ? upperBound.ToString() : null,
                lowerRange == RangeType.Inclusive,
                upperRange == RangeType.Inclusive);
        }

        public SortField CreateSortField(bool reverse)
        {
            throw new NotSupportedException();
        }

        public bool CaseSensitive { get; set; }
    }
}