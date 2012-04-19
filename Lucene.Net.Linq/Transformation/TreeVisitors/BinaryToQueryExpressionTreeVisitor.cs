using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class BinaryToQueryExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var queryType = QueryType.Default;
            var occur = BooleanClause.Occur.MUST;

            switch (expression.NodeType)
            {
                case ExpressionType.GreaterThan:
                    queryType = QueryType.GreaterThan;
                    break;
                case ExpressionType.LessThan:
                    queryType = QueryType.LessThan;
                    break;
                case ExpressionType.Equal:
                    break;
                case ExpressionType.NotEqual:
                    occur = BooleanClause.Occur.MUST_NOT;
                    break;
                default:
                    return base.VisitBinaryExpression(expression);
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