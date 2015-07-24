using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneRangeQueryExpression : Expression
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
        {
            this.field = field;
            this.lower = lower;
            this.lowerQueryType = lowerQueryType;
            this.upper = upper;
            this.upperQueryType = upperQueryType;
            this.occur = occur;
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.LuceneRangeQueryExpression; }
        }

        public override Type Type
        {
            get { return typeof(bool); }
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

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.Visit(field);
            var newLower = visitor.Visit(lower);
            var newUpper = visitor.Visit(upper);

            return (newField == field && newLower == lower && newUpper == upper) ? this :
                new LuceneRangeQueryExpression(newField, newLower, lowerQueryType, newUpper, upperQueryType, occur);
        }

        public override string ToString()
        {
            return string.Format("{0}LuceneRangeQuery({1} {2} TO {3} {4}", occur, lowerQueryType, lower, upperQueryType, upper);
        }
    }
}
