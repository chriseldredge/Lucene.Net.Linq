using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Expressions
{
    internal class LuceneQueryFieldExpression : ExtensionExpression
    {
        private readonly string fieldName;
        public const ExpressionType ExpressionType = (ExpressionType)150001;

        internal LuceneQueryFieldExpression(Type type, string fieldName)
            : base(type, ExpressionType)
        {
            this.fieldName = fieldName;
        }

        internal LuceneQueryFieldExpression(Type type, ExpressionType expressionType, string fieldName)
            : base(type, expressionType)
        {
            this.fieldName = fieldName;
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            // no children.
            return this;
        }

        public string FieldName { get { return fieldName; } }

        public bool Equals(LuceneQueryFieldExpression other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.fieldName, fieldName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (LuceneQueryFieldExpression)) return false;
            return Equals((LuceneQueryFieldExpression) obj);
        }

        public override int GetHashCode()
        {
            return (fieldName != null ? fieldName.GetHashCode() : 0);
        }

        public static bool operator ==(LuceneQueryFieldExpression left, LuceneQueryFieldExpression right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LuceneQueryFieldExpression left, LuceneQueryFieldExpression right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return "LuceneField(" + fieldName + ")";
        }
    }
}