using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq
{
    
    public class QueryBuildingExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private readonly Context context;
        private readonly Stack<Query> queries = new Stack<Query>();

        internal QueryBuildingExpressionTreeVisitor(Context context)
        {
            this.context = context;
        }

        public Query Query
        {
            get
            {
                if (queries.Count == 0) return new MatchAllDocsQuery();
                var query = queries.Peek();
                if (query is BooleanQuery)
                {
                    var booleanQuery = (BooleanQuery)query.Clone();
                    // TODO: need to check recursively?
                    if (booleanQuery.GetClauses().All(c => c.GetOccur() == BooleanClause.Occur.MUST_NOT))
                    {
                        booleanQuery.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
                        return booleanQuery;
                    }
                }
                return query;
            }
        }

        public Query Parse(string fieldName, string pattern)
        {
            var queryParser = new QueryParser(context.Version, fieldName, context.Analyzer);
            queryParser.SetLowercaseExpandedTerms(false);
            queryParser.SetAllowLeadingWildcard(true);
            return queryParser.Parse(pattern);
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    return MakeBooleanQuery(expression);
                default:
                    throw new NotSupportedException("BinaryExpression of type " + expression.NodeType + " is not supported.");
            }
        }

        protected override Expression VisitExtensionExpression(Remotion.Linq.Clauses.Expressions.ExtensionExpression expression)
        {
            if (!(expression is LuceneQueryExpression))
            {
                return base.VisitExtensionExpression(expression);
            }

            var q = (LuceneQueryExpression) expression;

            bool isPrefixCoded;
            var pattern = EvaluateExpressionToString(q.QueryPattern, out isPrefixCoded);

            var occur = q.Occur;
            Query query = null;
            var fieldName = q.QueryField.FieldName;
            if (pattern == null)
            {
                pattern = "*";
                occur = Negate(occur);
            }
            else if (q.QueryType == QueryType.Prefix)
            {
                pattern += "*";
            }
            else if (q.QueryType == QueryType.GreaterThan || q.QueryType == QueryType.GreaterThanOrEqual)
            {
                var boundary = EvaluateExpression(q.QueryPattern, out isPrefixCoded);
                var range = q.QueryType == QueryType.GreaterThan ? RangeType.Exclusive : RangeType.Inclusive;
                query = NumericRangeUtils.CreateNumericRangeQuery(fieldName, (ValueType) boundary, null, range, RangeType.Inclusive);
            }
            else if (q.QueryType == QueryType.LessThan || q.QueryType == QueryType.LessThanOrEqual)
            {
                var boundary = EvaluateExpression(q.QueryPattern, out isPrefixCoded);
                var range = q.QueryType == QueryType.LessThan ? RangeType.Exclusive : RangeType.Inclusive;
                query = NumericRangeUtils.CreateNumericRangeQuery(fieldName, null, (ValueType)boundary, RangeType.Inclusive, range);
            }

            //TODO: allow client to use non-encoded textual queries on ints?

            if (query == null)
                query = isPrefixCoded ? new TermQuery(new Term(fieldName, pattern)) : Parse(fieldName, pattern);

            var booleanQuery = new BooleanQuery();

            booleanQuery.Add(query, occur);

            queries.Push(booleanQuery);

            return base.VisitExtensionExpression(expression);
        }

        private static BooleanClause.Occur Negate(BooleanClause.Occur occur)
        {
            return (occur == BooleanClause.Occur.MUST_NOT)
                       ? BooleanClause.Occur.MUST
                       : BooleanClause.Occur.MUST_NOT;
        }

        private Expression MakeBooleanQuery(BinaryExpression expression)
        {
            var result = base.VisitBinaryExpression(expression);

            var second = queries.Pop();
            var first = queries.Pop();

            var query = new BooleanQuery();
            var occur = expression.NodeType == ExpressionType.AndAlso ? BooleanClause.Occur.MUST : BooleanClause.Occur.SHOULD;
            query.Add(first, occur);
            query.Add(second, occur);
            
            queries.Push(query);

            return result;
        }

        private static object EvaluateExpression(Expression expression, out bool isPrefixCoded)
        {
            isPrefixCoded = false;

            var lambda = Expression.Lambda(expression).Compile();
            var result = lambda.DynamicInvoke();

            if (result is ValueType)
            {
                isPrefixCoded = true;
            }

            return result;
        }

        private static string EvaluateExpressionToString(Expression expression, out bool isPrefixCoded)
        {
            var result = EvaluateExpression(expression, out isPrefixCoded);

            if (isPrefixCoded) return ((ValueType) result).ToPrefixCoded();

            return result == null ? null : result.ToString();
        }
    }
}