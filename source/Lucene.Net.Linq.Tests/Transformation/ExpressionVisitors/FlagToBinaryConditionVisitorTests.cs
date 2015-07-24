using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.ExpressionVisitors
{
    [TestFixture]
    public class FlagToBinaryConditionVisitorTests
    {
        private FlagToBinaryConditionVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new FlagToBinaryConditionVisitor();
        }

        [Test]
        public void ConvertFlag()
        {
            // "where doc.SomeFlag"
            var expression = new LuceneQueryFieldExpression(typeof (bool), "SomeFlag");

            var result = visitor.Visit(expression) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Left, Is.SameAs(expression));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)result.Right).Value, Is.EqualTo(true));
        }

        [Test]
        public void ConvertFlagNestedInCompoundBinaryLeft()
        {
            // where doc.SomeFlag && doc.Name == "foo"
            var flag = new LuceneQueryFieldExpression(typeof(bool), "SomeFlag");
            var binary = Expression.MakeBinary(ExpressionType.Equal, new LuceneQueryFieldExpression(typeof(string), "Name"), Expression.Constant("foo"));
            var topBinary = Expression.MakeBinary(ExpressionType.AndAlso, flag, binary);

            var result = visitor.Visit(topBinary) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Right, Is.SameAs(binary));
            Assert.That(result.Left, Is.InstanceOf<BinaryExpression>());
            Assert.That(((BinaryExpression)result.Left).Left, Is.SameAs(flag));
            Assert.That(((BinaryExpression)result.Left).Right, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void ConvertFlagNestedInCompoundBinaryRight()
        {
            // where doc.Name == "foo" || doc.SomeFlag
            var flag = new LuceneQueryFieldExpression(typeof(bool), "SomeFlag");
            var binary = Expression.MakeBinary(ExpressionType.Equal, new LuceneQueryFieldExpression(typeof(string), "Name"), Expression.Constant("foo"));
            var topBinary = Expression.MakeBinary(ExpressionType.OrElse, binary, flag);

            var result = visitor.Visit(topBinary) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Left, Is.SameAs(binary));
            Assert.That(result.Right, Is.InstanceOf<BinaryExpression>());
            Assert.That(((BinaryExpression)result.Right).Left, Is.SameAs(flag));
            Assert.That(((BinaryExpression)result.Right).Right, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void IgnoresWhenAlreadyInBinaryExpression()
        {
            // where doc.SomeFlag == true
            var flag = new LuceneQueryFieldExpression(typeof(bool), "SomeFlag");
            var binary = Expression.MakeBinary(ExpressionType.Equal, flag, Expression.Constant(true));

            var result = (BinaryExpression)visitor.Visit(binary);

            Assert.That(result, Is.SameAs(binary));
        }

        [Test]
        public void Inverse()
        {
            // "where !doc.SomeFlag"
            var flag = new LuceneQueryFieldExpression(typeof(bool), "SomeFlag");
            var expression = Expression.MakeUnary(ExpressionType.Not, flag, typeof(bool));
            var result = visitor.Visit(expression) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Left, Is.SameAs(flag));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)result.Right).Value, Is.EqualTo(false));
        }

        [Test]
        public void ConvertNestedMethodCall()
        {
            // where !doc.Name.StartsWith("foo")
            var field = new LuceneQueryFieldExpression(typeof(string), "Name");
            var startsWith = Expression.Call(field, "StartsWith", null, Expression.Constant("foo"));
            var expression = Expression.MakeUnary(ExpressionType.Not, startsWith, typeof(bool));
            var result = visitor.Visit(expression) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Left, Is.SameAs(startsWith));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(result.NodeType, Is.EqualTo(ExpressionType.Equal));
            Assert.That(((ConstantExpression)result.Right).Value, Is.EqualTo(false));
        }

        [Test]
        public void DoubleInverse()
        {
            // "where !(!doc.SomeFlag)"
            var flag = new LuceneQueryFieldExpression(typeof(bool), "SomeFlag");
            var expression = Expression.MakeUnary(ExpressionType.Not, flag, typeof(bool));
            expression = Expression.MakeUnary(ExpressionType.Not, expression, typeof(bool));
            var result = visitor.Visit(expression) as BinaryExpression;

            Assert.That(result, Is.Not.Null, "Expected BinaryExpression to be returned.");
            Assert.That(result.Left, Is.SameAs(flag));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)result.Right).Value, Is.EqualTo(true));
        }

    }
}
