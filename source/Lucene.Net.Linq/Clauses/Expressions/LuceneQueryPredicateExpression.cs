using System.Linq.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneQueryPredicateExpression : ExtensionExpression
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
            : base(typeof(bool), (ExpressionType)LuceneExpressionType.LuceneQueryPredicateExpression)
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

        public Occur Occur
        {
            get { return occur; }
        }

        public float Boost
        {
            get { return field.Boost; }
            set { field.Boost = value; }
        }

        public QueryType QueryType
        {
            get { return queryType; }
        }

        public bool AllowSpecialCharacters { get; set; }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            var newField = (LuceneQueryFieldExpression)visitor.VisitExpression(QueryField);
            var newPattern = visitor.VisitExpression(QueryPattern);

            return (newPattern == QueryPattern && newField == QueryField) ? this : new LuceneQueryPredicateExpression(newField, newPattern, Occur) { AllowSpecialCharacters = AllowSpecialCharacters };
        }

        public override string ToString()
        {
            return string.Format("LuceneQuery[{0}]({1}{2}:{3}){4}{5}", QueryType, Occur, QueryField.FieldName, pattern, Boost - 1.0f < 0.01f ? "" : "^" + Boost, AllowSpecialCharacters ? ".AllowSpecialCharacters()" : "");
        }
    }
}