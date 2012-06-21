using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneOrderByRelevanceExpression : ExtensionExpression
    {
        private static readonly LuceneOrderByRelevanceExpression instance = new LuceneOrderByRelevanceExpression();

        private LuceneOrderByRelevanceExpression()
            : base(typeof(object), (ExpressionType)LuceneExpressionType.LuceneOrderByRelevanceExpression)
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