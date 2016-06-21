using System;
using System.Linq.Expressions;

namespace Lucene.Net.Linq.Clauses.Expressions
{
    internal class LuceneQueryFieldExpression : Expression
    {
        private readonly string fieldName;
        private readonly Type type;
        private readonly ExpressionType nodeType;

        internal LuceneQueryFieldExpression(Type type, string fieldName)
            : this(type, (ExpressionType) LuceneExpressionType.LuceneQueryFieldExpression, fieldName)
        {
            Boost = 1;
        }

        internal LuceneQueryFieldExpression(Type type, ExpressionType nodeType, string fieldName)
        {
            this.type = type;
            this.nodeType = nodeType;
            this.fieldName = fieldName;
        }

        public override ExpressionType NodeType
        {
            get { return nodeType; }
        }

        public override Type Type
        {
            get { return type; }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            // no children.
            return this;
        }

        public string FieldName { get { return fieldName; } }
        public float Boost { get; set; }

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
            var s = "LuceneField(" + fieldName + ")";
            if (Math.Abs(Boost - 1.0f) > 0.01f)
            {
                return s + "^" + Boost;
            }
            return s;
        }
    }
}
