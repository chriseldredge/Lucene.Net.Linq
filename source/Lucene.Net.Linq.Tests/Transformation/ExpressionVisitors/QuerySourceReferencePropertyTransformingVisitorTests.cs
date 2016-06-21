using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using NUnit.Framework;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Lucene.Net.Linq.Tests.Transformation.ExpressionVisitors
{
    [TestFixture]
    public class QuerySourceReferencePropertyTransformingVisitorTests
    {
        private QuerySourceReferencePropertyTransformingVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new QuerySourceReferencePropertyTransformingVisitor();
        }

        [Test]
        public void Simple()
        {
            var expr = Expression.Property(new QuerySourceReferenceExpression(new MainFromClause("i", typeof(Record), Expression.Constant("r"))), "Name");

            var result = visitor.Visit(expr);

            Assert.That(result, Is.EqualTo(new LuceneQueryFieldExpression(typeof(string), "Name")));
        }

        [Test]
        public void Nested()
        {
            // where this.Convert(r).Name == "x"
            var queryRef = new QuerySourceReferenceExpression(new MainFromClause("i", typeof(Record), Expression.Constant("r")));
            var call = Expression.Call(Expression.Constant(this), GetType().GetMethod("Convert"), queryRef);
            var prop = Expression.Property(call, "Name");

            var result = visitor.Visit(prop);

            Assert.That(result, Is.EqualTo(new LuceneQueryFieldExpression(typeof(string), "Name")));
        }

        public OtherRecord Convert(Record r)
        {
            return new OtherRecord { Name = r.Name };
        }
    }
}
