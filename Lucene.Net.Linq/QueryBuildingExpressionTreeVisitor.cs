using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
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
            else if (q.QueryType == QueryType.GreaterThan)
            {
                var boundary = EvaluateExpression(q.QueryPattern, out isPrefixCoded);
                query = CreateNumericRangeQuery(fieldName, boundary, null, false, true);
            }
            else if (q.QueryType == QueryType.LessThan)
            {
                var boundary = EvaluateExpression(q.QueryPattern, out isPrefixCoded);
                query = CreateNumericRangeQuery(fieldName, null, boundary, true, false);
            }

            if (query == null)
                query = isPrefixCoded ? new TermQuery(new Term(fieldName, pattern)) : Parse(fieldName, pattern);

            var booleanQuery = new BooleanQuery();

            booleanQuery.Add(query, occur);

            queries.Push(booleanQuery);

            return base.VisitExtensionExpression(expression);
        }

        private NumericRangeQuery CreateNumericRangeQuery(string fieldName, object lower, object upper, bool lowerInclusive, bool upperInclusive)
        {
            if (lower == null)
            {
                lower = upper.GetType().GetField("MinValue").GetValue(null);
            }
            else if (upper == null)
            {
                upper = lower.GetType().GetField("MaxValue").GetValue(null);
            }

            lower = Convert(lower);
            upper = Convert(upper);

            if (lower is int)
            {
                return NumericRangeQuery.NewIntRange(fieldName, (int)lower, (int)upper, lowerInclusive, upperInclusive);    
            }
            else if (lower is long)
            {
                return NumericRangeQuery.NewLongRange(fieldName, (long)lower, (long)upper, lowerInclusive, upperInclusive);    
            }
            
            throw new NotSupportedException("Unsupported numeric range type " + lower.GetType());
        }

        private static ValueType Convert(object value)
        {
            if (value is DateTime)
            {
                return ((DateTime) value).ToUniversalTime().Ticks;
            }
            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).Ticks; 
            }
            if (value is bool)
            {
                return ((bool) value) ? 1 : 0;
            }

            return (ValueType) value;
        }

        private BooleanClause.Occur Negate(BooleanClause.Occur occur)
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

        internal static object EvaluateExpression(Expression expression, out bool isPrefixCoded)
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

        internal static string EvaluateExpressionToString(Expression expression, out bool isPrefixCoded)
        {
            var result = EvaluateExpression(expression, out isPrefixCoded);

            if (isPrefixCoded) return ConvertToPrefixCoded((ValueType) result);

            return result == null ? null : result.ToString();
        }

        private static string ConvertToPrefixCoded(ValueType result)
        {
            result = Convert(result);
            //TODO: allow client to use non-encoded textual queries on ints?
            if (result is int)
            {
                return NumericUtils.IntToPrefixCoded((int) result);
            }
            if (result is long)
            {
                return NumericUtils.LongToPrefixCoded((long)result);
            }

            throw new NotSupportedException("ValueType " + result.GetType() + " not supported.");
        }
    }
}