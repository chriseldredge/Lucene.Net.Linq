using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.TreeVisitors
{
    internal abstract class LuceneExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
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

            return base.VisitExtensionExpression(expression);
        }

        protected virtual Expression VisitBoostBinaryExpression(BoostBinaryExpression expression)
        {
            var binary = VisitExpression(expression.BinaryExpression);

            if (ReferenceEquals(expression.BinaryExpression, binary)) return expression;

            return new BoostBinaryExpression((BinaryExpression) binary, expression.Boost);
        }

        protected virtual Expression VisitLuceneRangeQueryExpression(LuceneRangeQueryExpression expression)
        {
            var lower = VisitExpression(expression.Lower);
            var upper = VisitExpression(expression.Upper);
            var field = VisitExpression(expression.QueryField);

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
            var field = (LuceneQueryFieldExpression)VisitExpression(expression.QueryField);
            var pattern = VisitExpression(expression.QueryPattern);

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
