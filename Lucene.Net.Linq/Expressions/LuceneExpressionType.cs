namespace Lucene.Net.Linq.Expressions
{
    internal enum LuceneExpressionType
    {
        LuceneQueryFieldExpression = 150001,
        LuceneQueryPredicateExpression,
        LuceneCompositeOrderingExpression,
        LuceneOrderByRelevanceExpression,
        LuceneQueryAnyFieldExpression,
        BoostBinaryExpression,
        LuceneQueryExpression
    }
}