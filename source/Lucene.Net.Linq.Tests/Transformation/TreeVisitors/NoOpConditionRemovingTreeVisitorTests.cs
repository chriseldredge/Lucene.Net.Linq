using System.Linq.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class NoOpConditionRemovingTreeVisitorTests
    {
        private NoOpConditionRemovingTreeVisitor visitor;
        private Expression predicate;

        [SetUp]
        public void SetUp()
        {
            visitor = new NoOpConditionRemovingTreeVisitor();

            predicate = Expression.MakeMemberAccess(Expression.Constant(this), GetType().GetProperty("Flag"));
        }

        public bool Flag { get; set; }

        [Test]
        public void Convert_TrueAndAlso()
        {
            // "where true && PredicateExpression"
            var expression = Expression.MakeBinary(
                ExpressionType.AndAlso,
                Expression.Constant(true),
                predicate);

            var result = visitor.VisitExpression(expression);

            Assert.That(result, Is.SameAs(predicate));
        }

        [Test]
        public void Convert_FalseOrElse()
        {
            // "where false || PredicateExpression"
            var expression = Expression.MakeBinary(
                ExpressionType.OrElse,
                Expression.Constant(false),
                predicate);

            var result = visitor.VisitExpression(expression);

            Assert.That(result, Is.SameAs(predicate));
        }

        [Test]
        public void Convert_Recursive()
        {
            // "where true && (false || PredicateExpression)"
            var inner = Expression.MakeBinary(
                ExpressionType.OrElse,
                Expression.Constant(false),
                predicate);
            var expression = Expression.MakeBinary(
                ExpressionType.AndAlso,
                Expression.Constant(true),
                inner);

            var result = visitor.VisitExpression(expression);

            Assert.That(result, Is.SameAs(predicate));
        }

    }
}