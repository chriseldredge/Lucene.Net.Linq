using System.Linq.Expressions;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.ExpressionVisitors
{
    [TestFixture]
    public class NullSafetyConditionRemovingVisitorTests
    {
        private NullSafetyConditionRemovingVisitor visitor;
        private readonly ConstantExpression X = Expression.Constant("x");
        private readonly ConstantExpression Y = Expression.Constant("y");
        private readonly ConstantExpression Null = Expression.Constant(null, typeof(string));

        [SetUp]
        public void SetUp()
        {
            visitor = new NullSafetyConditionRemovingVisitor();
        }

        [Test]
        public void NotEqual_LeftSide()
        {
            // x != null ? x : null
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, X, Null),
                X,
                Null);

            var result = visitor.Visit(condition);

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

            var result = visitor.Visit(condition);

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

            var result = visitor.Visit(condition);

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

            var result = visitor.Visit(condition);

            Assert.That(result, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void ComparesExpressionsByReflection()
        {
            // null == x ? null : x
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal, Null, X),
                Null,
                Expression.Constant("x"));

            var result = visitor.Visit(condition);

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

            var result = visitor.Visit(condition);

            Assert.That(result, Is.SameAs(condition), "Should not simplify when false condition returns non-null.");
        }

        [Test]
        public void NonNullResultNotEqualToTestArgument()
        {
            // y != null ? x : null
            var condition = Expression.Condition(
                Expression.MakeBinary(ExpressionType.NotEqual, Y, Null),
                X,
                Null);

            var result = visitor.Visit(condition);

            Assert.That(result, Is.SameAs(X));
        }

        [Test]
        public void Recursive()
        {
            // y == null ? null : (x == null ? null : x.CompareTo(y))
            var concat = Expression.Call(typeof(string).GetMethod("Concat", new[] {typeof(string), typeof(string)}), X, Y);

            var nested = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal, X, Null),
                Null,
                concat);

            var outer = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal, Y, Null),
                Null,
                nested);

            var result = visitor.Visit(outer);

            Assert.That(result, Is.SameAs(concat));
        }

        [Test]
        public void Boolean()
        {
            // true == false ? null : X
            var outer = Expression.Condition(
                Expression.MakeBinary(ExpressionType.Equal,
                    Expression.Constant(true),
                    Expression.Constant(false)),
                Null,
                X);

            var result = visitor.Visit(outer);

            Assert.That(result, Is.SameAs(X));
        }

    }
}
