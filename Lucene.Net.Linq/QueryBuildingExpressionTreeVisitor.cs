using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
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
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    break;
                default:
                    throw new NotSupportedException("BinaryExpression of type " + expression.NodeType + " is not supported.");
            }

            var queryLocator = new QueryLocatingVisitor();
            string field;
            Expression patternExpression;

            if (queryLocator.FindQueryFieldName(expression.Left))
            {
                field = queryLocator.FieldName;
                patternExpression = expression.Right;
            }
            else if (queryLocator.FindQueryFieldName(expression.Right))
            {
                field = queryLocator.FieldName;
                patternExpression = expression.Left;
            }
            else
            {
                throw new NotSupportedException("Failed to map left or right side of BinaryExpression to a field.");
            }

            bool isPrefixCoded;
            var pattern = EvaluateExpression(patternExpression, out isPrefixCoded);

            var occur = BooleanClause.Occur.MUST_NOT;
            var shouldCreateBooleanClause = pattern == null || expression.NodeType == ExpressionType.NotEqual;

            if (pattern == null)
            {
                pattern = "*";
                if (expression.NodeType == ExpressionType.NotEqual)
                {
                    occur = BooleanClause.Occur.MUST;
                }
            }

            var query = isPrefixCoded ? new TermQuery(new Term(field, pattern)) : Parse(field, pattern);

            if (shouldCreateBooleanClause)
            {
                var booleanQuery = new BooleanQuery();
                
                booleanQuery.Add(query, occur);
                query = booleanQuery;
            }

            queries.Push(query);
            
            return base.VisitBinaryExpression(expression);
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

        internal static string EvaluateExpression(Expression expression, out bool isPrefixCoded)
        {
            isPrefixCoded = false;

            var lambda = Expression.Lambda(expression).Compile();
            var result = lambda.DynamicInvoke();
            
            if (result is ValueType)
            {
                isPrefixCoded = true;
                return ConvertToPrefixCoded((ValueType)result);
            }

            return result == null ? null : result.ToString();
        }

        private static string ConvertToPrefixCoded(ValueType result)
        {
            //TODO: allow client to use non-encoded textual queries on ints?
            if (result is Int32)
            {
                return NumericUtils.IntToPrefixCoded((int) result);
            }

            if (result is Boolean)
            {
                return NumericUtils.IntToPrefixCoded(((bool) result) ? 1 : 0);
            }

            throw new NotSupportedException("ValueType " + result.GetType() + " not supported.");
        }
    }
}