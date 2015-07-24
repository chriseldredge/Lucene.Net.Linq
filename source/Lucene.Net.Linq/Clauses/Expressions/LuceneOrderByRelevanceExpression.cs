using System;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneOrderByRelevanceExpression : Expression
    {
        private static readonly LuceneOrderByRelevanceExpression instance = new LuceneOrderByRelevanceExpression();

        private LuceneOrderByRelevanceExpression()
        {
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.LuceneOrderByRelevanceExpression; }
        }

        public override Type Type
        {
            get { return typeof (object); }
        }

        public static Expression Instance
        {
            get { return instance; }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}
