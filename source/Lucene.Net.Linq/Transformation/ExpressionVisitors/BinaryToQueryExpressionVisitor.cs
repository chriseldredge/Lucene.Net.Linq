using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal class BinaryToQueryExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression expression)
        {
            QueryType queryType;

            if (!expression.NodeType.TryGetQueryType(out queryType))
            {
                return base.VisitBinary(expression);
            }

            var occur = Occur.MUST;
            if (expression.NodeType == ExpressionType.NotEqual)
            {
                occur = Occur.MUST_NOT;
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

                switch(queryType)
                {
                    case QueryType.GreaterThan:
                        queryType = QueryType.LessThan;
                        break;
                    case QueryType.LessThan:
                        queryType = QueryType.GreaterThan;
                        break;
                    case QueryType.GreaterThanOrEqual:
                        queryType = QueryType.LessThanOrEqual;
                        break;
                    case QueryType.LessThanOrEqual:
                        queryType = QueryType.GreaterThanOrEqual;
                        break;
                }
            }
            else
            {
                throw new NotSupportedException("Expected Left or Right to be LuceneQueryFieldExpression");
            }

            return new LuceneQueryPredicateExpression(fieldExpression, pattern, occur, queryType);
        }
    }
}
