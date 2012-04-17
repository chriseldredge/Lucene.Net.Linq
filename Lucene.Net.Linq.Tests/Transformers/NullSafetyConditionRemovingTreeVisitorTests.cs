using System.Linq.Expressions;
using Lucene.Net.Linq.Transformers;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformers
{
    [TestFixture]
    public class NullSafetyConditionRemovingTreeVisitorTests
    {
        private NullSafetyConditionRemovingTreeVisitor visitor;
        private readonly ConstantExpression X = Expression.Constant("x");
        private readonly ConstantExpression Y = Expression.Constant("y");
        private readonly ConstantExpression Null = Expression.Constant(null, typeof(string));

        [SetUp]
        public void SetUp()
        {
            visitor = new NullSafetyConditionRemovingTreeVisitor();
        }

        [Test]
        public void NotEqual_LeftSide()
        {
            // x != null ? x : null
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, X, Null),
                X,
                Null);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void NotEqual_RightSide()
        {
            // null != x ? null : x
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, Null, X),
                Null,
                X);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void Equal_LeftSide()
        {
            // null == x ? null : x
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal, Null, X),
                Null,
                X);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void Equal_RightSide()
        {
            // x == null ? null : x
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal, X, Null),
                Null,
                X);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void IgnoresNonNullResult()
        {
            // x != null ? x : y
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, X, Null),
                X,
                Y);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.SameAs(condition), "Should not simplify when false condition returns non-null.");
        }

        [Test]
        public void IgnoresResultNotEqualToTestArgument()
        {
            // y != null ? x : null
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, Y, Null),
                X,
                Null);

            var result = visitor.VisitExpression(condition);

            Assert.That(result, Is.SameAs(condition), "Should not simplify when true condition returns a different expression.");
        }
    }
}
