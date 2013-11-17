using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneRangeQueryExpression : ExtensionExpression
    {
        private readonly LuceneQueryFieldExpression field;
        private readonly Expression lower;
        private readonly QueryType lowerQueryType;
        private readonly Expression upper;
        private readonly QueryType upperQueryType;
        private Occur occur;

        public LuceneRangeQueryExpression(LuceneQueryFieldExpression field, Expression lower, QueryType lowerQueryType, Expression upper, QueryType upperQueryType)
            : this(field, lower, lowerQueryType, upper, upperQueryType, Occur.MUST)
        {
        }

        public LuceneRangeQueryExpression(LuceneQueryFieldExpression field, Expression lower, QueryType lowerQueryType, Expression upper, QueryType upperQueryType, Occur occur)
            : base(typeof(bool), (ExpressionType)LuceneExpressionType.LuceneRangeQueryExpression)
        {
            this.field = field;
            this.lower = lower;
            this.lowerQueryType = lowerQueryType;
            this.upper = upper;
            this.upperQueryType = upperQueryType;
            this.occur = occur;
        }

        public LuceneQueryFieldExpression QueryField
        {
            get { return field; }
        }

        public Expression Lower
        {
            get { return lower; }
        }

        public QueryType LowerQueryType
        {
            get { return lowerQueryType; }
        }

        public Expression Upper
        {
            get { return upper; }
        }

        public QueryType UpperQueryType
        {
            get { return upperQueryType; }
        }

        public Occur Occur
        {
            get { return occur; }
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.VisitExpression(field);
            var newLower = visitor.VisitExpression(lower);
            var newUpper = visitor.VisitExpression(upper);

            return (newField == field && newLower == lower && newUpper == upper) ? this :
                new LuceneRangeQueryExpression(newField, newLower, lowerQueryType, newUpper, upperQueryType, occur);
        }

        public override string ToString()
        {
            return string.Format("{0}LuceneRangeQuery({1} {2} TO {3} {4}", occur, lowerQueryType, lower, upperQueryType, upper);
        }
    }
}