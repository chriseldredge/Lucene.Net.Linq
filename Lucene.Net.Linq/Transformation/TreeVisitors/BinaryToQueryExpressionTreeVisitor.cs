using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class BinaryToQueryExpressionTreeVisitor : ExpressionTreeVisitor
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

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            QueryType queryType;
            if (!typeMap.TryGetValue(expression.NodeType, out queryType))
            {
                return base.VisitBinaryExpression(expression);
            }

            var occur = BooleanClause.Occur.MUST;
            if (expression.NodeType == ExpressionType.NotEqual)
            {
                occur = BooleanClause.Occur.MUST_NOT;
            }

            LuceneQueryFieldExpression fieldExpression;
            Expression pattern;

            if (expression.Left is LuceneQueryFieldExpression)
            {
                fieldExpression = (LuceneQueryFieldExpression) expression.Left;
                pattern = expression.Right;
            }
            else if (expression.Right is LuceneQueryFieldExpression)
            {
                fieldExpression = (LuceneQueryFieldExpression) expression.Right;
                pattern = expression.Left;
            }
            else
            {
                throw new NotSupportedException("Expected Left or Right to be LuceneQueryFieldExpression");
            }

            return new LuceneQueryExpression(fieldExpression, pattern, occur, queryType);
        }
    }
}