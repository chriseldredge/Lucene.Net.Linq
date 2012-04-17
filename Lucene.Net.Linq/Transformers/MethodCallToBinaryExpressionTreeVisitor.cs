using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformers
{
    /// <summary>
    /// Replaces supported method calls like [LuceneQueryFieldExpression].StartsWith("foo") with a BinaryExpression like [LuceneQueryFieldExpression] == foo*
    /// </summary>
    internal class MethodCallToBinaryExpressionTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (!(expression.Object is LuceneQueryFieldExpression))
                return base.VisitMethodCallExpression(expression);

            if (expression.Method.Name == "StartsWith")
            {
                bool prefixCoded;
                // TODO: evaluate expression ahead of time so it will always be a ConstantExpression.
                var right = QueryBuildingExpressionTreeVisitor.EvaluateExpression(expression.Arguments[0], out prefixCoded) + "*";
                
                return Expression.MakeBinary(ExpressionType.Equal, expression.Object, Expression.Constant(right));
            }
            return base.VisitMethodCallExpression(expression);
        }
    }
}