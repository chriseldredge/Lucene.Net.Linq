namespace Lucene.Net.Linq.Search
{
    public enum QueryType
    {
        Default,
        Prefix,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    public enum RangeType
    {
        Exclusive,
        Inclusive
    }
}