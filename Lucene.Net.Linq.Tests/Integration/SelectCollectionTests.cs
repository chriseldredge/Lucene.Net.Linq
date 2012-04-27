using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SelectCollectionTests : IntegrationTestBase
    {
        class TaggedDocument
        {
            public string Name { get; set; }

            [Field("tag")]
            public IEnumerable<string> Tags { get; set; }
        }

        [Test]
        public void EnumerableContains()
        {
            using (var session = provider.OpenSession<TaggedDocument>())
            {
                session.Add(new TaggedDocument { Name = "First", Tags = new[] {"a", "b"}});
                session.Add(new TaggedDocument { Name = "Second", Tags = new[] {"c", "d"}});
                session.Commit();
            }

            var documents = provider.AsQueryable<TaggedDocument>();

            var result = from doc in documents where doc.Tags.Contains("c") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Second"));
        }
    }
}