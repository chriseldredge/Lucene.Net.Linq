using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class BoostBinaryExpression : ExtensionExpression
    {
        private readonly BinaryExpression expression;
        private readonly float boost;
        
        public BoostBinaryExpression(BinaryExpression expression, float boost)
            : base(expression.Type, (ExpressionType)LuceneExpressionType.BoostBinaryExpression)
        {
            this.expression = expression;
            this.boost = boost;
        }

        public BinaryExpression BinaryExpression
        {
            get { return expression; }
        }

        public float Boost
        {
            get { return boost; }
        }

        public override string ToString()
        {
            return string.Format("{0}^{1}", BinaryExpression, Boost);
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newExpression = visitor.VisitExpression(BinaryExpression);

            if (ReferenceEquals(BinaryExpression, newExpression)) return this;

            return new BoostBinaryExpression((BinaryExpression) newExpression, Boost);
        }
    }
}