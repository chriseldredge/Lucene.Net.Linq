using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class AllowSpecialCharactersMethodExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private readonly Stack<ExtensionExpression> ancestors = new Stack<ExtensionExpression>();

        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            var custom = expression as AllowSpecialCharactersExpression;

            if (custom == null)
            {
                try
                {
                    ancestors.Push(expression);
                    return base.VisitExtensionExpression(expression);    
                }
                finally
                {
                    ancestors.Pop();
                }
            }

            return VisitAllowSpecialCharactersExpression(custom);

        }

        private Expression VisitAllowSpecialCharactersExpression(AllowSpecialCharactersExpression expression)
        {
            const string message = "Expected AllowSpecialCharactersExpression to appear within a LuceneQueryPredicateExpression.";

            if (ancestors.Count == 0)
            {
                throw new InvalidOperationException(message);
            }

            var query = ancestors.Peek() as LuceneQueryPredicateExpression;

            if (query == null)
            {
                throw new InvalidOperationException(message);
            }

            query.AllowSpecialCharacters = true;

            return expression.Pattern;
        }
    }
}