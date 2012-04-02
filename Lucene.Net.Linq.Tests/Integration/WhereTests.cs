using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class WhereTests : IntegrationTestBase
    {
        [SetUp]
        public void AddDocuments()
        {
            AddDocument("0", "apple");
            AddDocument("1", "banana");
            AddDocument("2", "cherries are red");
            AddDocument("Example.Package.1.4.100.nupkg");
        }

        [Test]
        public void IdEquals()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("id") == "0" select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("0"));
        }

        [Test]
        public void TextEquals()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("text") == "banana" select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("1"));
        }

        [Test]
        public void TextEqualsWithPunctuation()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("id") == "Example.Package.1.4.100.nupkg" select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("Example.Package.1.4.100.nupkg"));
        }

        [Test]
        public void TextEqualsFuzzyMatch()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("text") == "Cherry is RED" select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("2"));
        }

        [Test, Ignore("How to support Contains?")]
        public void ContainsWord()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("text").Contains("cherry") select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("2"));
        }

        [Test, Ignore("TODO: method call on method call")]
        public void StartsWith()
        {
            var result = from doc in provider.AsQueryable() where doc.Get("text").StartsWith("ban") select doc.Get("id");

            Assert.That(result.FirstOrDefault(), Is.EqualTo("1"));
        }
    }


}