using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Tests.Translation.TreeVisitors;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class QuerySourceReferencePropertyTransformingTreeVisitorTests
    {
        private QuerySourceReferencePropertyTransformingTreeVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new QuerySourceReferencePropertyTransformingTreeVisitor();
        }

        [Test]
        public void Simple()
        {
            var expr = Expression.Property(new QuerySourceReferenceExpression(new MainFromClause("i", typeof(Record), Expression.Constant("r"))), "Name");

            var result = visitor.VisitExpression(expr);

            Assert.That(result, Is.EqualTo(new LuceneQueryFieldExpression(typeof(string), "Name")));
        }

        [Test]
        public void Nested()
        {
            // where this.Convert(r).Name == "x"
            var queryRef = new QuerySourceReferenceExpression(new MainFromClause("i", typeof(Record), Expression.Constant("r")));
            var call = Expression.Call(Expression.Constant(this), GetType().GetMethod("Convert"), queryRef);
            var prop = Expression.Property(call, "Name");

            var result = visitor.VisitExpression(prop);

            Assert.That(result, Is.EqualTo(new LuceneQueryFieldExpression(typeof(string), "Name")));
        }

        public OtherRecord Convert(Record r)
        {
            return new OtherRecord { Name = r.Name };
        }
    }

    public class OtherRecord
    {
        public string Name { get; set; }
    }
}