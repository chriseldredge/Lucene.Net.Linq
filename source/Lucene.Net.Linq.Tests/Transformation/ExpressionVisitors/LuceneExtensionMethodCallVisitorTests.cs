using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.ExpressionVisitors
{
    [TestFixture]
    public class LuceneExtensionMethodCallVisitorTests
    {
        private LuceneExtensionMethodCallVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new LuceneExtensionMethodCallVisitor();
        }

        public string Name { get; set; }

        [Test]
        public void OrderByScore()
        {
            var doc = new object();

            // [doc].Score()
            var call = Expression.Call(typeof(LuceneMethods), "Score", new[] { doc.GetType() }, Expression.Constant(doc));

            var result = visitor.Visit(call);

            Assert.That(result, Is.InstanceOf<LuceneOrderByRelevanceExpression>());
        }

        [Test]
        public void AnyField()
        {
            var doc = new object();

            // [doc].AnyField() == "foo"
            var expression = Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Call(typeof(LuceneMethods), "AnyField", new[] { doc.GetType() }, Expression.Constant(doc)),
                Expression.Constant("foo"));

            var result = visitor.Visit(expression) as BinaryExpression;

            Assert.That(result.Left, Is.InstanceOf<LuceneQueryAnyFieldExpression>());
        }
    }
}
