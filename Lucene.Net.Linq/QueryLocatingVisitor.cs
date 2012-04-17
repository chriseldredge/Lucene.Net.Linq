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

        public string FieldName
        {
            get { return fieldName; }
        }

        public bool FindQueryFieldName(Expression expression)
        {
            ancestors.Add(expression);
            VisitExpression(expression);
            ancestors.Clear();
            return fieldName != null;
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