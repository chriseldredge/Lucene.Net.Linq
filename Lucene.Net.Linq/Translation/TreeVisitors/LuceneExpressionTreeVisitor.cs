using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Translation.TreeVisitors
{
    internal abstract class LuceneExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            if (expression is LuceneQueryAnyFieldExpression)
            {
                return VisitLuceneQueryAnyFieldExpression((LuceneQueryAnyFieldExpression)expression);
            }

            if (expression is LuceneQueryFieldExpression)
            {
                return VisitLuceneQueryFieldExpression((LuceneQueryFieldExpression) expression);
            }

            if (expression is LuceneQueryExpression)
            {
                return VisitLuceneQueryExpression((LuceneQueryExpression) expression);
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

        protected virtual Expression VisitLuceneQueryExpression(LuceneQueryExpression expression)
        {
            var field = (LuceneQueryFieldExpression)VisitExpression(expression.QueryField);
            var pattern = VisitExpression(expression.QueryPattern);

            if (field != expression.QueryField || pattern != expression.QueryPattern)
            {
                return new LuceneQueryExpression(field, pattern, expression.Occur, expression.QueryType);
            }

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
