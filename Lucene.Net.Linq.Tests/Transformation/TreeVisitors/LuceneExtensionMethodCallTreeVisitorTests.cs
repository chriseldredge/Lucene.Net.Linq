using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class LuceneExtensionMethodCallTreeVisitorTests
    {
        private LuceneExtensionMethodCallTreeVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new LuceneExtensionMethodCallTreeVisitor();
        }

        public string Name { get; set; }

        [Test]
        public void OrderByScore()
        {
            var doc = new object();

            // [doc].Score()
            var call = Expression.Call(typeof(LuceneMethods), "Score", new[] { doc.GetType() }, Expression.Constant(doc));

            var result = visitor.VisitExpression(call);

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

            var result = visitor.VisitExpression(expression) as BinaryExpression;

            Assert.That(result.Left, Is.InstanceOf<LuceneQueryAnyFieldExpression>());
        }
    }
}