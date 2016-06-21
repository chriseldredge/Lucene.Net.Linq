using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;

namespace Lucene.Net.Linq.Clauses.ExpressionVisitors
{
    internal abstract class LuceneExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression expression)
        {
            if (expression is LuceneQueryExpression)
            {
                return VisitLuceneQueryExpression((LuceneQueryExpression)expression);
            }

            if (expression is LuceneQueryAnyFieldExpression)
            {
                return VisitLuceneQueryAnyFieldExpression((LuceneQueryAnyFieldExpression)expression);
            }

            if (expression is LuceneQueryFieldExpression)
            {
                return VisitLuceneQueryFieldExpression((LuceneQueryFieldExpression) expression);
            }

            if (expression is LuceneRangeQueryExpression)
            {
                return VisitLuceneRangeQueryExpression((LuceneRangeQueryExpression) expression);
            }

            if (expression is LuceneQueryPredicateExpression)
            {
                return VisitLuceneQueryPredicateExpression((LuceneQueryPredicateExpression) expression);
            }

            if (expression is BoostBinaryExpression)
            {
                return VisitBoostBinaryExpression((BoostBinaryExpression) expression);
            }

            return base.VisitExtension(expression);
        }

        protected virtual Expression VisitBoostBinaryExpression(BoostBinaryExpression expression)
        {
            var binary = Visit(expression.BinaryExpression);

            if (ReferenceEquals(expression.BinaryExpression, binary)) return expression;

            return new BoostBinaryExpression((BinaryExpression) binary, expression.Boost);
        }

        protected virtual Expression VisitLuceneRangeQueryExpression(LuceneRangeQueryExpression expression)
        {
            var lower = Visit(expression.Lower);
            var upper = Visit(expression.Upper);
            var field = Visit(expression.QueryField);

            if (ReferenceEquals(lower, expression.Lower) && ReferenceEquals(upper, expression.Upper)
                && ReferenceEquals(field, expression.QueryField))
            {
                return expression;
            }

            return new LuceneRangeQueryExpression((LuceneQueryFieldExpression) field,
                (LuceneQueryPredicateExpression)lower, expression.LowerQueryType,
                (LuceneQueryPredicateExpression)upper, expression.UpperQueryType);
        }

        protected virtual Expression VisitLuceneQueryPredicateExpression(LuceneQueryPredicateExpression expression)
        {
            var field = (LuceneQueryFieldExpression)Visit(expression.QueryField);
            var pattern = Visit(expression.QueryPattern);

            if (field != expression.QueryField || pattern != expression.QueryPattern)
            {
                return new LuceneQueryPredicateExpression(field, pattern, expression.Occur, expression.QueryType);
            }

            return expression;
        }

        protected virtual Expression VisitLuceneQueryExpression(LuceneQueryExpression expression)
        {
            return expression;
        }

        protected virtual Expression VisitLuceneQueryAnyFieldExpression(LuceneQueryAnyFieldExpression expression)
        {
            return expression;
        }

        protected virtual Expression VisitLuceneQueryFieldExpression(LuceneQueryFieldExpression expression)
        {
            return expression;
        }
    }
}
