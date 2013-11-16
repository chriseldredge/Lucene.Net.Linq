using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class RangeQueryMergeExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            if (expression.NodeType != ExpressionType.AndAlso)
            {
                return expression;
            }

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

            if (IsLowerBoundRangeQuery(right.QueryType) && IsUpperBoundRangeQuery(left.QueryType))
            {
                var tmp = left;
                left = right;
                right = tmp;
            }

            if (IsLowerBoundRangeQuery(left.QueryType) && IsUpperBoundRangeQuery(right.QueryType))
            {
                return new LuceneRangeQueryExpression(left.QueryField, left.QueryPattern, left.QueryType, right.QueryPattern, right.QueryType);
            }

            return base.VisitBinaryExpression(expression);
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