using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces supported method calls like Compare([LuceneQueryFieldExpression], "abc") to LuceneQueryExpression
    /// </summary>
    internal class CompareCallToBinaryExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private static readonly ISet<ExpressionType> compareTypes =
            new HashSet<ExpressionType>
            {
                ExpressionType.GreaterThan,
                ExpressionType.GreaterThanOrEqual,
                ExpressionType.LessThan,
                ExpressionType.LessThanOrEqual
            };

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            if (!compareTypes.Contains(expression.NodeType) || !IsCompareMethod(expression.Left) || !expression.Right.IsZeroConstant())
            {
                return base.VisitBinaryExpression(expression);
            }

            return ConvertToQueryExpression(expression.NodeType, (MethodCallExpression) expression.Left) ?? base.VisitBinaryExpression(expression);
        }

        private LuceneQueryExpression ConvertToQueryExpression(ExpressionType compareType, MethodCallExpression expression)
        {
            if (expression.Arguments[0] is LuceneQueryFieldExpression)
            {
                return new LuceneQueryExpression((LuceneQueryFieldExpression) expression.Arguments[0], expression.Arguments[1], BooleanClause.Occur.MUST, compareType.ToQueryType());
            }

            if (expression.Arguments[1] is LuceneQueryFieldExpression)
            {
                return new LuceneQueryExpression((LuceneQueryFieldExpression)expression.Arguments[1], expression.Arguments[0], BooleanClause.Occur.MUST, compareType.ToQueryType());
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