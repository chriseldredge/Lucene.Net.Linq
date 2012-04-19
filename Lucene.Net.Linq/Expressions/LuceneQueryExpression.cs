using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Expressions
{
    internal class LuceneQueryExpression : ExtensionExpression
    {
        public const ExpressionType ExpressionType = (ExpressionType)150002;

        private readonly LuceneQueryFieldExpression field;
        private readonly Expression pattern;
        private readonly BooleanClause.Occur occur;
        private readonly QueryType queryType;

        public LuceneQueryExpression(LuceneQueryFieldExpression field, Expression pattern, BooleanClause.Occur occur)
            : this(field, pattern, occur, QueryType.Default)
        {
        }

        public LuceneQueryExpression(LuceneQueryFieldExpression field, Expression pattern, BooleanClause.Occur occur, QueryType queryType)
            : base(typeof(bool), ExpressionType)
        {
            this.field = field;
            this.pattern = pattern;
            this.occur = occur;
            this.queryType = queryType;
        }

        public LuceneQueryFieldExpression QueryField
        {
            get { return field; }
        }

        public Expression QueryPattern
        {
            get { return pattern; }
        }

        public BooleanClause.Occur Occur
        {
            get { return occur; }
        }

        public QueryType QueryType
        {
            get { return queryType; }
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.VisitExpression(QueryField);
            var newPattern = visitor.VisitExpression(QueryPattern);

            return (newPattern == QueryPattern && newField == QueryField) ? this : new LuceneQueryExpression(newField, newPattern, Occur);
        }

    }
}