using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SelectTests : IntegrationTestBase
    {
        [Test]
        public void SelectDocument()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s;

            Assert.That(result.First().Get("id"), Is.EqualTo("a"));
        }

        [Test]
        public void SelectWithIdentityMethod()
        {
            AddDocument("a");
            AddDocument("b");

            var sample = provider.AsQueryable();

            var result = from s in sample select IdentityMethod(s);

            Assert.That(result.ToArray().Select(d => d.Get("id")), Is.EqualTo(new[] {"a", "b"}));
        }

        [Test]
        public void SelectWithTransformingMethod()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select TransformingMethod(s);

            Assert.That(result.First(), Is.EqualTo("a"));
        }

        [Test]
        public void SelectField()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s.Get("id");

            Assert.That(result.First(), Is.EqualTo("a"));
        }

        [Test]
        public void SelectComplex()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s.Get("id") + " suffix";

            Assert.That(result.First(), Is.EqualTo("a suffix"));
        }

        private static Document IdentityMethod(Document document)
        {
            return document;
        }

        private static object TransformingMethod(Document document)
        {
            return document.Get("id");
        }
    }

}
