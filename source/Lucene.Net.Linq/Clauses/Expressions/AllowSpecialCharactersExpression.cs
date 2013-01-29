using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class AllowSpecialCharactersExpression : ExtensionExpression
    {
        private readonly Expression pattern;

        internal AllowSpecialCharactersExpression(Expression pattern)
            : base(pattern.Type, (ExpressionType)LuceneExpressionType.AllowSpecialCharactersExpression)
        {
            this.pattern = pattern;
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newPattern = visitor.VisitExpression(pattern);

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