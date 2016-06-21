using System;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class AllowSpecialCharactersExpression : Expression
    {
        private readonly Expression pattern;

        internal AllowSpecialCharactersExpression(Expression pattern)
        {
            this.pattern = pattern;
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.AllowSpecialCharactersExpression; }
        }

        public override Type Type
        {
            get { return pattern.Type; }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newPattern = visitor.Visit(pattern);

            if (Equals(pattern, newPattern)) return this;

            return new AllowSpecialCharactersExpression(newPattern);
        }

        public Expression Pattern
        {
            get { return pattern; }
        }

        public override string ToString()
        {
            return pattern + ".AllowSpecialCharacters()";
        }
    }
}
