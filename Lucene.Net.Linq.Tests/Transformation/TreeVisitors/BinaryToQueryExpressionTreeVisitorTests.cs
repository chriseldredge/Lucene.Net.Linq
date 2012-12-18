﻿using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class BinaryToQueryExpressionTreeVisitorTests
    {
        private BinaryToQueryExpressionTreeVisitor visitor;

        [SetUp]
        public void SetUp()
        {
            visitor = new BinaryToQueryExpressionTreeVisitor();
        }

        [Test]
        public void IgnoresUnsupportedNodeTypes()
        {
            var expression = Expression.MakeBinary(ExpressionType.Modulo, Expression.Constant(10), Expression.Constant(3));

            Assert.That(visitor.VisitExpression(expression), Is.SameAs(expression));
        }

        [Test]
        public void GreaterThan_SwitchesOperatorWhenConstantIsOnLeft()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.GreaterThan, five, new LuceneQueryFieldExpression(typeof(int), "Count"));

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.LessThan, Occur.MUST);
        }

        [Test]
        public void LessThan_SwitchesOperatorWhenConstantIsOnLeft()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.LessThan, five, new LuceneQueryFieldExpression(typeof(int), "Count"));

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.GreaterThan, Occur.MUST);
        }

        [Test]
        public void GreaterThanOrEqual_SwitchesOperatorWhenConstantIsOnLeft()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, five, new LuceneQueryFieldExpression(typeof(int), "Count"));

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.LessThanOrEqual, Occur.MUST);
        }

        [Test]
        public void LessThanOrEqual_SwitchesOperatorWhenConstantIsOnLeft()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.LessThanOrEqual, five, new LuceneQueryFieldExpression(typeof(int), "Count"));

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.GreaterThanOrEqual, Occur.MUST);
        }

        [Test]
        public void Equal()
        {
            var foo = Expression.Constant("foo");
            var expression = Expression.MakeBinary(ExpressionType.Equal, new LuceneQueryFieldExpression(typeof(string), "Name"), foo);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Name", foo, QueryType.Default, Occur.MUST);
        }

        [Test]
        public void NotEqual()
        {
            var foo = Expression.Constant("foo");
            var expression = Expression.MakeBinary(ExpressionType.NotEqual, new LuceneQueryFieldExpression(typeof(string), "Name"), foo);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Name", foo, QueryType.Default, Occur.MUST_NOT);
        }

        [Test]
        public void GreaterThan()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.GreaterThan, new LuceneQueryFieldExpression(typeof(int), "Count"), five);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.GreaterThan, Occur.MUST);
        }
        
        [Test]
        public void GreaterThanOrEqual()
        {
            var time = Expression.Constant(new DateTime(2012, 4, 18));
            var expression = Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, new LuceneQueryFieldExpression(typeof(DateTime), "Time"), time);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Time", time, QueryType.GreaterThanOrEqual, Occur.MUST);
        }

        [Test]
        public void LessThan()
        {
            var five = Expression.Constant(5);
            var expression = Expression.MakeBinary(ExpressionType.LessThan, new LuceneQueryFieldExpression(typeof(int), "Count"), five);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Count", five, QueryType.LessThan, Occur.MUST);
        }

        [Test]
        public void LessThanOrEqual()
        {
            var five = Expression.Constant(5.125d);
            var expression = Expression.MakeBinary(ExpressionType.LessThanOrEqual, new LuceneQueryFieldExpression(typeof(double), "Average"), five);

            var result = visitor.VisitExpression(expression);
            AssertLuceneQueryExpression(result, "Average", five, QueryType.LessThanOrEqual, Occur.MUST);
        }

        [Test]
        public void ThrowsOnMissingQueryField()
        {
            var expression = Expression.MakeBinary(ExpressionType.NotEqual, Expression.Constant("bar"), Expression.Constant("foo"));

            TestDelegate call = () => visitor.VisitExpression(expression);
            Assert.That(call, Throws.Exception.InstanceOf<NotSupportedException>());
        }

        private void AssertLuceneQueryExpression(Expression expression, string expectedQueryFieldName, ConstantExpression expectedPatternExpression, QueryType expectedQueryType, Occur expectedOccur)
        {
            Assert.That(expression, Is.InstanceOf<LuceneQueryPredicateExpression>());
            var result = (LuceneQueryPredicateExpression)expression;

            Assert.That(result.Occur, Is.EqualTo(expectedOccur));
            Assert.That(result.QueryType, Is.EqualTo(expectedQueryType));
            Assert.That(result.QueryField.FieldName, Is.EqualTo(expectedQueryFieldName));
            Assert.That(result.QueryPattern, Is.EqualTo(expectedPatternExpression));
        }
    }
}