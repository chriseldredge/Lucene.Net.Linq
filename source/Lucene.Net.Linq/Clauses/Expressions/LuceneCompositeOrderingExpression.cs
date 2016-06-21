using System.Collections.Generic;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneCompositeOrderingExpression : Expression
    {
        private readonly IEnumerable<LuceneQueryFieldExpression> fields;

        public LuceneCompositeOrderingExpression(IEnumerable<LuceneQueryFieldExpression> fields)
            : base((ExpressionType)LuceneExpressionType.LuceneCompositeOrderingExpression, typeof(object))
        {
            this.fields = fields;
        }

        public IEnumerable<LuceneQueryFieldExpression> Fields
        {
            get { return fields; }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}
