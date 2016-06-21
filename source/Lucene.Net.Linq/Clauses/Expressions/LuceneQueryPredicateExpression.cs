using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneQueryPredicateExpression : Expression
    {
        private readonly LuceneQueryFieldExpression field;
        private readonly Expression pattern;
        private readonly Occur occur;
        private readonly QueryType queryType;

        public LuceneQueryPredicateExpression(LuceneQueryFieldExpression field, Expression pattern, Occur occur)
            : this(field, pattern, occur, QueryType.Default)
        {
        }

        public LuceneQueryPredicateExpression(LuceneQueryFieldExpression field, Expression pattern, Occur occur, QueryType queryType)
        {
            this.field = field;
            this.pattern = pattern;
            this.occur = occur;
            this.queryType = queryType;
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)LuceneExpressionType.LuceneQueryPredicateExpression; }
        }

        public override Type Type
        {
            get { return typeof(bool); }
        }

        public LuceneQueryFieldExpression QueryField
        {
            get { return field; }
        }

        public Expression QueryPattern
        {
            get { return pattern; }
        }

        public Occur Occur
        {
            get { return occur; }
        }

        public float Boost
        {
            get { return field.Boost; }
            set { field.Boost = value; }
        }

        public float? Fuzzy
        {
            get;
            set;
        }

        public QueryType QueryType
        {
            get { return queryType; }
        }

        public bool AllowSpecialCharacters { get; set; }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.Visit(QueryField);
            var newPattern = visitor.Visit(QueryPattern);

            return (newPattern == QueryPattern && newField == QueryField) ? this : new LuceneQueryPredicateExpression(newField, newPattern, Occur) { AllowSpecialCharacters = AllowSpecialCharacters };
        }

        public override string ToString()
        {
            return string.Format("LuceneQuery[{0}]({1}{2}:{3}){4}{5}", QueryType, Occur, QueryField.FieldName, pattern, Boost - 1.0f < 0.01f ? "" : "^" + Boost, AllowSpecialCharacters ? ".AllowSpecialCharacters()" : "");
        }
    }
}
