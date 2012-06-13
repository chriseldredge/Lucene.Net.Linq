using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class MethodInfoMatchingTreeVisitorTests
    {
        private static readonly Expression SupportedMethodReplacement = Expression.Constant("replaced supported method");
        private static readonly Expression ConstantReplacement = Expression.Constant("replaced constant");
        
        private TestableVisitor visitor;

        class TestableVisitor : MethodInfoMatchingTreeVisitor
        {
            protected override Expression VisitSupportedMethodCallExpression(MethodCallExpression expression)
            {
                return SupportedMethodReplacement;
            }

            protected override Expression VisitConstantExpression(ConstantExpression expression)
            {
                return ConstantReplacement;
            }
        }

        [SetUp]
        public void SetUp()
        {
            visitor = new TestableVisitor();
        }

        [Test]
        public void DelegatesToBaseByDefault()
        {
            var call = Expression.Call(typeof (string).GetMethod("Copy"), Expression.Constant("input"));

            var result = (MethodCallExpression)visitor.VisitExpression(call);

            Assert.That(result.Arguments[0], Is.SameAs(ConstantReplacement));
        }

        [Test]
        public void DelegatesToSupportedMethodOnMatch()
        {
            var methodInfo = typeof (string).GetMethod("Copy");
            visitor.AddMethod(methodInfo);
            var call = Expression.Call(methodInfo, Expression.Constant("input"));

            var result = visitor.VisitExpression(call);

            Assert.That(result, Is.SameAs(SupportedMethodReplacement));
        }
    }
}