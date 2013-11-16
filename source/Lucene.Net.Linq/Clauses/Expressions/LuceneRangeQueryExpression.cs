using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
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

        public LuceneRangeQueryExpression(LuceneQueryFieldExpression field, Expression lower, QueryType lowerQueryType, Expression upper, QueryType upperQueryType)
            : base(typeof(bool), (ExpressionType)LuceneExpressionType.LuceneRangeQueryExpression)
        {
            this.field = field;
            this.lower = lower;
            this.lowerQueryType = lowerQueryType;
            this.upper = upper;
            this.upperQueryType = upperQueryType;
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

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.VisitExpression(field);
            var newLower = visitor.VisitExpression(lower);
            var newUpper = visitor.VisitExpression(upper);

            return (newField == field && newLower == lower && newUpper == upper) ? this :
                new LuceneRangeQueryExpression(newField, newLower, lowerQueryType, newUpper, upperQueryType);
        }

        public override string ToString()
        {
            return string.Format("LuceneRangeQuery({0} {1} TO {2} {3}", lowerQueryType, lower, upperQueryType, upper);
        }
    }
}