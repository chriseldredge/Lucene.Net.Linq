using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneQueryAnyFieldExpression : LuceneQueryFieldExpression
    {
        private static readonly LuceneQueryAnyFieldExpression instance = new LuceneQueryAnyFieldExpression();

        private LuceneQueryAnyFieldExpression()
            : base(typeof(string), (ExpressionType)LuceneExpressionType.LuceneQueryAnyFieldExpression, "*")
        {
        }

        public static Expression Instance
        {
            get { return instance; }
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            return this;
        }
    }
}