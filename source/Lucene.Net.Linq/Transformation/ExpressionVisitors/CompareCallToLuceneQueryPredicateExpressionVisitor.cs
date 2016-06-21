using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    /// <summary>
    /// Replaces supported method calls like <c>string.Compare([LuceneQueryFieldExpression], "abc") > 0</c> to LuceneQueryPredicateExpression
    /// </summary>
    internal class CompareCallToLuceneQueryPredicateExpressionVisitor : ExpressionVisitor
    {
        private static readonly ISet<ExpressionType> compareTypes =
            new HashSet<ExpressionType>
            {
                ExpressionType.GreaterThan,
                ExpressionType.GreaterThanOrEqual,
                ExpressionType.LessThan,
                ExpressionType.LessThanOrEqual
            };

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            if (!compareTypes.Contains(expression.NodeType) || !IsCompareMethod(expression.Left) || !expression.Right.IsZeroConstant())
            {
                return base.VisitBinary(expression);
            }

            return ConvertToQueryExpression(expression.NodeType, (MethodCallExpression) expression.Left) ?? base.VisitBinary(expression);
        }

        private LuceneQueryPredicateExpression ConvertToQueryExpression(ExpressionType compareType, MethodCallExpression expression)
        {
            if (expression.Arguments[0] is LuceneQueryFieldExpression)
            {
                return new LuceneQueryPredicateExpression((LuceneQueryFieldExpression) expression.Arguments[0], expression.Arguments[1], Occur.MUST, compareType.ToQueryType());
            }

            if (expression.Arguments[1] is LuceneQueryFieldExpression)
            {
                return new LuceneQueryPredicateExpression((LuceneQueryFieldExpression)expression.Arguments[1], expression.Arguments[0], Occur.MUST, compareType.ToQueryType());
            }
            
            return null;
        }

        protected bool IsCompareMethod(Expression expression)
        {
            if (expression is MethodCallExpression)
            {
                var mc = (MethodCallExpression) expression;
                return mc.Method.Name == "Compare" && mc.Arguments.Count == 2;
            }

            return false;
        }

        
    }
}