using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class IndexBoostTests : IntegrationTestBase
    {
        public class BoostDocument
        {
            [Field(Analyzer = typeof(KeywordAnalyzer), Boost = 2f)]
            public string Title { get; set; }

            [Field(Analyzer = typeof(KeywordAnalyzer))]
            public string Body { get; set; }

            [NumericField(Boost = 20f)]
            public int Popularity { get; set; }

            [QueryScore]
            public float Score { get; set; }
        }

        [Test]
        public void NormalFieldBoost()
        {
            AddDocument(new BoostDocument { Title = "car", Body = "truck"});
            AddDocument(new BoostDocument { Title = "truck", Body = "auto"});

            var result = from doc in provider.AsQueryable<BoostDocument>()
                         where doc.Body == "truck" || doc.Title == "truck"
                         select doc;
            
            Assert.That(result.First().Title, Is.EqualTo("truck"));
            Assert.That(result.OrderByDescending(doc => doc.Score()).First().Title, Is.EqualTo("car"));
        }

        [Test]
        [Ignore("See https://issues.apache.org/jira/browse/LUCENENET-519 (NumericField.Boost ignored at index time)")]
        public void NumericFieldBoost()
        {
            AddDocument(new BoostDocument { Body = "5", Popularity = 0 });
            AddDocument(new BoostDocument { Body = "five", Popularity = 5 });

            var result = from doc in provider.AsQueryable<BoostDocument>()
                         where doc.Body == "5" || doc.Popularity == 5
                         select doc;

            Console.WriteLine(result.First().Score);
            Console.WriteLine(result.Last().Score);
            Assert.That(result.OrderBy(doc => doc.Score()).First().Body, Is.EqualTo("five"));
            Assert.That(result.OrderByDescending(doc => doc.Score()).First().Body, Is.EqualTo("5"));
        }
    }
}