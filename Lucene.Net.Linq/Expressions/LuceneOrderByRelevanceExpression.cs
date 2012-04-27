using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Expressions
{
    internal class LuceneOrderByRelevanceExpression : ExtensionExpression
    {
        private static readonly LuceneOrderByRelevanceExpression instance = new LuceneOrderByRelevanceExpression();
        public const ExpressionType ExpressionType = (ExpressionType)150004;

        private LuceneOrderByRelevanceExpression()
            : base(typeof(object), ExpressionType)
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