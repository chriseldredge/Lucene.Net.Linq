using System.Collections.Generic;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Search
{
    public enum QueryType
    {
        Default,
        Prefix,
        Suffix,
        Wildcard,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual 
    }

    public enum RangeType
    {
        Exclusive,
        Inclusive
    }

    public static class ExpressionTypeExtentions
    {
        private static readonly IDictionary<ExpressionType, QueryType> typeMap =
               new Dictionary<ExpressionType, QueryType>
                {
                    {ExpressionType.GreaterThan, QueryType.GreaterThan},
                    {ExpressionType.GreaterThanOrEqual, QueryType.GreaterThanOrEqual},
                    {ExpressionType.LessThan, QueryType.LessThan},
                    {ExpressionType.LessThanOrEqual, QueryType.LessThanOrEqual},
                    {ExpressionType.Equal, QueryType.Default},
                    {ExpressionType.NotEqual, QueryType.Default},
                };

        public static QueryType ToQueryType(this ExpressionType type)
        {
            return typeMap[type];
        }

        public static bool TryGetQueryType(this ExpressionType type, out QueryType queryType)
        {
            return typeMap.TryGetValue(type, out queryType);
        }
    }
}