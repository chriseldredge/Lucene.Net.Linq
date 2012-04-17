using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq
{
    public class QueryLocatingVisitor : ExpressionTreeVisitor
    {
        private readonly List<Expression> ancestors = new List<Expression>();
        private string fieldName;
        private string pattern;
        private bool prefixCoded;

        public string FieldName
        {
            get { return fieldName; }
        }

        public string Pattern
        {
            get { return pattern; }
        }

        public bool PrefixCoded
        {
            get { return prefixCoded; }
        }

        public bool FindQueryFieldName(Expression expression)
        {
            ancestors.Add(expression);
            VisitExpression(expression);
            ancestors.Clear();
            return fieldName != null;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            var parent = ancestors[0];
            var grandParent = ancestors.Count > 1 ? ancestors[1] : parent;

            if (parent is MemberExpression)
            {
                fieldName = ((MemberExpression) parent).Member.Name;

                if (parent != grandParent)
                {
                    if (grandParent is MethodCallExpression)
                    {
                        pattern = GetQueryPatternFromMethodCall((MethodCallExpression) grandParent);
                    }
                    else
                    {
                        throw new InvalidOperationException("Expected expression in the form of [" +
                                                            expression.ReferencedQuerySource.ItemName + "].PropertyName");
                    }
                }
            }
            else if (parent is MethodCallExpression)
            {
                var me = (MethodCallExpression) parent;
                if (me.Method.Name == "Get")
                {
                    fieldName = QueryBuildingExpressionTreeVisitor.EvaluateExpression(me.Arguments[0], out prefixCoded);
                }
            }

            return base.VisitQuerySourceReferenceExpression(expression);
        }

        private string GetQueryPatternFromMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "StartsWith")
            {
                return QueryBuildingExpressionTreeVisitor.EvaluateExpression(expression.Arguments[0], out prefixCoded) + "*";
            }

            throw new InvalidOperationException("Unsupported query method " + expression.Method.Name);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            ancestors.Insert(0, expression);
            var result = base.VisitMemberExpression(expression);
            ancestors.RemoveAt(0);
            return result;
        }

        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            var queryField = expression as LuceneQueryFieldExpression;
            if (queryField != null)
            {
                fieldName = queryField.FieldName;
            }
            return base.VisitExtensionExpression(expression);
        }
    }
}