using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class BoostTests : IntegrationTestBase
    {
        private IQueryable<SampleDocument> documents;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "sample", Id = "0", Scalar = 0});
            AddDocument(new SampleDocument { Name = "other", Id = "1", Scalar = 1});

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void Boost()
        {
            var query = from d in documents where d.Name == "sample" || d.Id.Boost(0) == "0" select d;

            Assert.That(query.First().Name, Is.EqualTo("sample"));
        }

        [Test]
        public void Boost_MethodCall()
        {
            var query = from d in documents where d.Name == "sample" || d.Name.Boost(2f).StartsWith("other") select d;

            Assert.That(query.First().Name, Is.EqualTo("other"));
        }

        [Test]
        public void Boost_BinaryExpression()
        {
            var query = from d in documents where d.Name == "other" || (d.Id == "0").Boost(100) select d;

            Assert.That(query.First().Name, Is.EqualTo("sample"));
        }

        [Test]
        public void Boost_CompoundBinaryExpression()
        {
            var query = from d in documents where (d.Name == "other" || (d.Id == "1")).Boost(0) || d.Name == "sample" select d;

            Assert.That(query.First().Name, Is.EqualTo("sample"));
        }

        [Test]
        public void Dynamic_Single()
        {
            var first = documents.Boost(d => d.Scalar);

            Assert.That(first.ToList()[0].Id, Is.EqualTo("1"));
        }

        [Test]
        public void Dynamic_Multiple()
        {
            AddDocument(new SampleDocument { Name = "sample", Id = "33", Scalar = 1 });

            var first = documents.Where(d => d.Name == "sample").Boost(d => d.Id.Length).Boost(d => d.Scalar);

            Assert.That(first.ToList()[0].Id, Is.EqualTo("33"));
        }

        [Test]
        public void ExtensionMethodThrowsWhenInvoked()
        {
            TestDelegate call = () => "hello".Boost(0f);

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void ThrowsWhenNotCalledOnQueryField()
        {
            TestDelegate call = () => (from d in documents orderby d.Name.Boost(5f) select d).ToList();
            Assert.That(call, Throws.InstanceOf<NotSupportedException>());
        }
    }
}