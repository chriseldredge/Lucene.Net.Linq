using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Expressions
{
    internal class LuceneQueryAnyFieldExpression : LuceneQueryFieldExpression
    {
        private static readonly LuceneQueryAnyFieldExpression instance = new LuceneQueryAnyFieldExpression();
        public new const ExpressionType ExpressionType = (ExpressionType)150005;

        private LuceneQueryAnyFieldExpression()
            : base(typeof(string), ExpressionType, "*")
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