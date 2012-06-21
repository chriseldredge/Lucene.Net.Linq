using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class MethodCallToBinaryExpressionTreeVisitorTests
    {
        private MethodCallToBinaryExpressionTreeVisitor visitor;
        private Expression field;
        private ConstantExpression foo;

        [SetUp]
        public void SetUp()
        {
            visitor = new MethodCallToBinaryExpressionTreeVisitor();
            field = new LuceneQueryFieldExpression(typeof (string), "firstName");
            foo = Expression.Constant("foo");
        }

        public string Name { get; set; }

        [Test]
        public void StartsWith()
        {
            var method = typeof (string).GetMethod("StartsWith", new[] { typeof(string) });

            // [doc].Name.StartsWith("foo")
            var call = Expression.Call(field, method, foo);

            var result = visitor.VisitExpression(call) as LuceneQueryPredicateExpression;

            Assert.That(result, Is.Not.Null, "result as LuceneQueryPredicateExpression");
            Assert.That(result.QueryField, Is.SameAs(field));
            Assert.That(result.QueryPattern, Is.EqualTo(foo));
            Assert.That(result.QueryType, Is.EqualTo(QueryType.Prefix));
        }

        [Test]
        public void EndsWith()
        {
            var method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });

            // [doc].Name.EndsWith("foo")
            var call = Expression.Call(field, method, foo);

            var result = visitor.VisitExpression(call) as LuceneQueryPredicateExpression;

            Assert.That(result, Is.Not.Null, "result as LuceneQueryPredicateExpression");
            Assert.That(result.QueryField, Is.SameAs(field));
            Assert.That(result.QueryPattern, Is.EqualTo(foo));
            Assert.That(result.QueryType, Is.EqualTo(QueryType.Suffix));
        }

        [Test]
        public void Contains()
        {
            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            
            // [doc].Name.Contains("foo")
            var call = Expression.Call(field, method, foo);

            var result = visitor.VisitExpression(call) as LuceneQueryPredicateExpression;

            Assert.That(result, Is.Not.Null, "result as LuceneQueryPredicateExpression");
            Assert.That(result.QueryField, Is.SameAs(field));
            Assert.That(result.QueryPattern, Is.EqualTo(foo));
            Assert.That(result.QueryType, Is.EqualTo(QueryType.Wildcard));
        }
    }
}