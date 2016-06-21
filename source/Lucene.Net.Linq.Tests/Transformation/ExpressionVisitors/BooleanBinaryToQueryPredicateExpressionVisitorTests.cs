using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.ExpressionVisitors
{
    [TestFixture]
    public class BooleanBinaryToQueryPredicateExpressionVisitorTests
    {
        private BooleanBinaryToQueryPredicateExpressionVisitor visitor;

        // Query(Name:foo*)
        private static readonly LuceneQueryPredicateExpression predicate = new LuceneQueryPredicateExpression(
            new LuceneQueryFieldExpression(typeof (string), "Name"),
            Expression.Constant("foo"),
            Occur.MUST,
            QueryType.Prefix);

        [SetUp]
        public void SetUp()
        {
            visitor = new BooleanBinaryToQueryPredicateExpressionVisitor();
        }

        [Test]
        public void EqualFalse()
        {
            var call = CreateBinaryExpression(ExpressionType.Equal, false);

            var result = visitor.Visit(call) as LuceneQueryPredicateExpression;

            AssertResult(result, Occur.MUST_NOT);
        }

        [Test]
        public void NotEqualTrue()
        {
            var call = CreateBinaryExpression(ExpressionType.NotEqual, true);

            var result = visitor.Visit(call) as LuceneQueryPredicateExpression;

            AssertResult(result, Occur.MUST_NOT);
        }

        [Test]
        public void EqualTrue()
        {
            var call = CreateBinaryExpression(ExpressionType.Equal, true);

            var result = visitor.Visit(call);

            Assert.That(result, Is.SameAs(predicate));
        }

        [Test]
        public void NotEqualFalse()
        {
            var call = CreateBinaryExpression(ExpressionType.NotEqual, false);

            var result = visitor.Visit(call);

            Assert.That(result, Is.SameAs(predicate));
        }
        [Test]
        public void RetainsBoostAndAllowSpecialCharacters()
        {
            var call = CreateBinaryExpression(ExpressionType.Equal, false);


            predicate.AllowSpecialCharacters = true;
            predicate.Boost = 1234f;

            var result = visitor.Visit(call) as LuceneQueryPredicateExpression;

            AssertResult(result, Occur.MUST_NOT);
        }

        private static void AssertResult(LuceneQueryPredicateExpression result, Occur expectedOccur)
        {
            Assert.That(result, Is.Not.Null, "Expected LuceneQueryPredicateExpression to be returned.");
            Assert.That(result, Is.Not.SameAs(predicate));
            Assert.That(result.QueryField, Is.SameAs(predicate.QueryField));
            Assert.That(result.QueryPattern, Is.SameAs(predicate.QueryPattern));
            Assert.That(result.QueryType, Is.EqualTo(predicate.QueryType));
            Assert.That(result.Occur, Is.EqualTo(expectedOccur));
            Assert.That(result.Boost, Is.EqualTo(predicate.Boost));
            Assert.That(result.AllowSpecialCharacters, Is.EqualTo(predicate.AllowSpecialCharacters));
        }

        private static BinaryExpression CreateBinaryExpression(ExpressionType expressionType, bool value)
        {
            return Expression.MakeBinary(
                expressionType,
                predicate,
                Expression.Constant(value));
        }
    }
}
