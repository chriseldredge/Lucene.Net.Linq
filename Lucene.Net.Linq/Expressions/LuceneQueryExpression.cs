using System;
using System.Linq.Expressions;
using Lucene.Net.Search;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Expressions
{
    internal class LuceneQueryExpression : ExtensionExpression
    {
        private readonly Query query;

        internal LuceneQueryExpression(Query query)
            : base(typeof(Query), (ExpressionType)LuceneExpressionType.LuceneQueryExpression)
        {
            this.query = query;
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            // no children.
            return this;
        }

        public Query Query
        {
            get { return query; }
        }
    }
}