using System;
using System.Linq.Expressions;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneQueryExpression : Expression
    {
        private readonly Query query;

        internal LuceneQueryExpression(Query query)
        {
            this.query = query;
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.LuceneRangeQueryExpression; }
        }

        public override Type Type
        {
            get { return typeof(Query); }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
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
