using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Transformation.TreeVisitors
{
    [TestFixture]
    public class NoOpMethodCallRemovingTreeVisitorTests
    {
        private NoOpMethodCallRemovingTreeVisitor visitor;
        private Expression target;

        [SetUp]
        public void SetUp()
        {
            visitor = new NoOpMethodCallRemovingTreeVisitor();
            target = Expression.Property(Expression.Constant(this), "Name");
        }

        public string Name { get; set; }

        [Test]
        public void ToLower()
        {
            var toLower = typeof (string).GetMethod("ToLower", new Type[0]);

            // this.Name.ToLower()
            var call = Expression.Call(target, toLower);

            var result = visitor.VisitExpression(call);

            Assert.That(result, Is.SameAs(target));
        }

        [Test]
        public void ToLowerInvariant()
        {
            var toLower = typeof(string).GetMethod("ToLowerInvariant", new Type[0]);

            // this.Name.ToLower()
            var call = Expression.Call(target, toLower);

            var result = visitor.VisitExpression(call);

            Assert.That(result, Is.SameAs(target));
        }
    }
}