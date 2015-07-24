using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class BoostBinaryExpression : Expression
    {
        private readonly BinaryExpression expression;
        private readonly float boost;

        public BoostBinaryExpression(BinaryExpression expression, float boost)
        {
            this.expression = expression;
            this.boost = boost;
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.BoostBinaryExpression; }
        }

        public override Type Type
        {
            get { return expression.Type; }
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

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newExpression = visitor.Visit(BinaryExpression);

            if (ReferenceEquals(BinaryExpression, newExpression)) return this;

            return new BoostBinaryExpression((BinaryExpression) newExpression, Boost);
        }
    }
}
