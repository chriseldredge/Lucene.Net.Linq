using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class NoOpConvertExpressionRemovingVisitorTests
    {
        private NoOpConvertExpressionRemovingVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new NoOpConvertExpressionRemovingVisitor();
        }

        [Test]
        public void ConvertNestedBinary()
        {
            var inner = Expression.MakeBinary(ExpressionType.Equal,
                Expression.Convert(Expression.Constant((bool?) false), typeof (bool)),
                Expression.Convert(Expression.Constant((bool?) false), typeof (bool)));

            var outer = Expression.MakeBinary(ExpressionType.AndAlso,
                Expression.Convert(Expression.Constant((bool?) false), typeof (bool)), inner);

            var result = (BinaryExpression) visitor.VisitExpression(outer);

            Assert.That(result.Left, Is.InstanceOf<ConstantExpression>());

            var innerResult = (BinaryExpression) result.Right;

            Assert.That(innerResult.Left, Is.InstanceOf<ConstantExpression>());
            Assert.That(innerResult.Right, Is.InstanceOf<ConstantExpression>());
        }

        public enum Enum { A, B }

        [Test]
        public void ConvertEnum()
        {
            var luceneQueryFieldExpression = new LuceneQueryFieldExpression(typeof(Enum), "field_name");
            var binary = Expression.MakeBinary(ExpressionType.Equal,
                Expression.Convert(luceneQueryFieldExpression, typeof(Int32)),
                Expression.Constant(0));

            var result = (BinaryExpression)visitor.VisitExpression(binary);

            Assert.That(result.Left, Is.SameAs(luceneQueryFieldExpression));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void ChangeTypeOfConstant()
        {
            var call = Expression.Call(Expression.Constant("hello"), "StartsWith", null, Expression.Constant("foo"));
            var expression = Expression.MakeBinary(ExpressionType.Equal,
                Expression.Convert(call, typeof (bool?)),
                Expression.Convert(Expression.Constant(false), typeof (bool?)));

            var result = (BinaryExpression) visitor.VisitExpression(expression);

            Assert.That(result.Left, Is.SameAs(call));
            Assert.That(result.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(result.Right.Type, Is.EqualTo(typeof (bool)));
            Assert.That(((ConstantExpression) result.Right).Value, Is.False);
        }
    }
}