using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces MemberExpression instances like [QuerySourceReferenceExpression].PropertyName with <c ref="LuceneQueryFieldExpression"/>
    /// </summary>
    internal class QuerySourceReferencePropertyTransformingTreeVisitor : ExpressionTreeVisitor
    {
        private MemberExpression parent;
        private LuceneQueryFieldExpression queryField;

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            parent = expression;
            
            var result = base.VisitMemberExpression(expression);

            return queryField ?? result;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            var propertyInfo = parent.Member as PropertyInfo;

            if (propertyInfo == null)
            {
                throw new NotSupportedException("Only MemberExpression of type PropertyInfo may be used on QuerySourceReferenceExpression.");
            }

            var propertyType = propertyInfo.PropertyType;
            if (propertyType.IsEnum)
            {
                propertyType = Enum.GetUnderlyingType(propertyType);
            }

            queryField = new LuceneQueryFieldExpression(propertyType, propertyInfo.Name);
            return base.VisitQuerySourceReferenceExpression(expression);
        }
    }
}