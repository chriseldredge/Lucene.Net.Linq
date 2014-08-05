using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class DocumentBoostTests : IntegrationTestBase
    {
        public class BoostDocument
        {
            [Field]
            public int Key { get; set; }

            [Field(Analyzer = typeof(KeywordAnalyzer))]
            public string Title { get; set; }

            [DocumentBoost]
            public float Boost { get; set; }

            [QueryScore]
            public float Score { get; set; }
        }

        [Test]
        public void DocumentBoost()
        {
            AddDocument(new BoostDocument { Key = 1, Title = "foo", Boost = 1f });
            AddDocument(new BoostDocument { Key = 2, Title = "foo", Boost = 2f });

            var result = from doc in provider.AsQueryable<BoostDocument>()
                         where doc.Title == "foo"
                         select doc;

            Assert.That(result.First().Key, Is.EqualTo(2));
            Assert.That(result.OrderByDescending(doc => doc.Score()).First().Key, Is.EqualTo(1));
        }
    }
}