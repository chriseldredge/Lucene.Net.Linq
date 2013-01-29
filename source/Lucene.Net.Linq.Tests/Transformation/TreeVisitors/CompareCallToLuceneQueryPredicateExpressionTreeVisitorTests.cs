using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class CompareCallToLuceneQueryPredicateExpressionTreeVisitorTests
    {
        private CompareCallToLuceneQueryPredicateExpressionTreeVisitor visitor;
        private readonly MethodInfo methodInfo = typeof(CompareCallToLuceneQueryPredicateExpressionTreeVisitorTests).GetMethod("Compare", BindingFlags.Static | BindingFlags.Public);

        private readonly Expression field = new LuceneQueryFieldExpression(typeof(string), "Name");
        private readonly Expression constant = Expression.Constant("John");

        [SetUp]
        public void SetUp()
        {
            visitor = new CompareCallToLuceneQueryPredicateExpressionTreeVisitor();
        }

        [Test]
        public void Compare()
        {
            // Compare([doc].Name, "John") > 0
            var call = 
                Expression.MakeBinary(
                    ExpressionType.GreaterThan,
                    Expression.Call(methodInfo, field, constant),
                    Expression.Constant(0));

            var result = visitor.VisitExpression(call) as LuceneQueryPredicateExpression;

            Assert.That(result, Is.Not.Null, "Expected LuceneQueryPredicateExpression to be returned.");
            Assert.That(result.QueryField, Is.SameAs(field));
            Assert.That(result.QueryPattern, Is.EqualTo(constant));
        }

        [Test]
        public void TransposedArguments()
        {
            // string.CompareTo("John", [doc].Name) > 0
            var call =
                Expression.MakeBinary(
                    ExpressionType.GreaterThan,
                    Expression.Call(methodInfo, constant, field),
                    Expression.Constant(0));

            var result = visitor.VisitExpression(call) as LuceneQueryPredicateExpression;

            Assert.That(result, Is.Not.Null, "Expected LuceneQueryPredicateExpression to be returned.");
            Assert.That(result.QueryField, Is.SameAs(field));
            Assert.That(result.QueryPattern, Is.EqualTo(constant));
        }

        public static int Compare(string a, string b)
        {
            // method signature meant to resemble
            // DataServiceProviderMethods.Compare()
            throw new NotImplementedException();
        }
    }
}