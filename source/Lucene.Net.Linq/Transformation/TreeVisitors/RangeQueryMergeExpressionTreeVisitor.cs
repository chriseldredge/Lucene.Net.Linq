using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class RangeQueryMergeExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var left = expression.Left as LuceneQueryPredicateExpression;
            var right = expression.Right as LuceneQueryPredicateExpression;

            if (left == null || right == null)
            {
                return expression;
            }

            if (left.QueryField != right.QueryField)
            {
                return expression;
            }

            var l = GetQueryPatternAsConstComparable(left);
            var r = GetQueryPatternAsConstComparable(right);

            if (l == null || r == null)
            {
                return expression;
            }

            if (IsLowerBoundRangeQuery(right.QueryType) && IsUpperBoundRangeQuery(left.QueryType))
            {
                if (l.CompareTo(r) < 0)
                {
                    if (expression.NodeType != ExpressionType.OrElse)
                    {
                        return expression;
                    }

                    return new LuceneRangeQueryExpression(left.QueryField, left.QueryPattern, Invert(left.QueryType), right.QueryPattern, Invert(right.QueryType), Occur.MUST_NOT);
                }

                var tmp = left;
                left = right;
                right = tmp;
            }
            else if (IsLowerBoundRangeQuery(left.QueryType) && IsUpperBoundRangeQuery(right.QueryType))
            {
                if (l.CompareTo(r) > 0)
                {
                    if (expression.NodeType != ExpressionType.OrElse)
                    {
                        return expression;
                    }

                    return new LuceneRangeQueryExpression(right.QueryField, right.QueryPattern, Invert(right.QueryType), left.QueryPattern, Invert(left.QueryType), Occur.MUST_NOT);
                }
            }

            if (expression.NodeType != ExpressionType.AndAlso)
            {
                return expression;
            }

            if (IsLowerBoundRangeQuery(left.QueryType) && IsUpperBoundRangeQuery(right.QueryType))
            {
                return new LuceneRangeQueryExpression(left.QueryField, left.QueryPattern, left.QueryType, right.QueryPattern, right.QueryType);
            }

            return base.VisitBinaryExpression(expression);
        }

        private static QueryType Invert(QueryType queryType)
        {
            switch (queryType)
            {
                case QueryType.GreaterThan:
                    return QueryType.LessThanOrEqual;
                case QueryType.GreaterThanOrEqual:
                    return QueryType.LessThan;
                case QueryType.LessThan:
                    return QueryType.GreaterThanOrEqual;
                case QueryType.LessThanOrEqual:
                    return QueryType.GreaterThan;
            }

            throw new ArgumentException("Cannot invert query type " + queryType, "queryType");
        }

        private static IComparable GetQueryPatternAsConstComparable(LuceneQueryPredicateExpression expression)
        {
            var constant = expression.QueryPattern as ConstantExpression;
            if (constant == null) return null;
            return constant.Value as IComparable;
        }

        private static bool IsLowerBoundRangeQuery(QueryType queryType)
        {
            return queryType == QueryType.GreaterThan || queryType == QueryType.GreaterThanOrEqual;
        }

        private static bool IsUpperBoundRangeQuery(QueryType queryType)
        {
            return queryType == QueryType.LessThan || queryType == QueryType.LessThanOrEqual;
        }
    }
}