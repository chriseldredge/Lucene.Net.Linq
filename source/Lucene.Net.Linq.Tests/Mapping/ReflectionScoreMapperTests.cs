using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class ReflectionScoreMapperTests
    {
        private ReflectionScoreMapper<Sample> mapper;
        private QueryExecutionContext queryExecutionContext;
        private Document document;
        private Sample sample;

        public class Sample
        {
            [QueryScore]
            public float Score { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            document = new Document();
            sample = new Sample();
            queryExecutionContext = new QueryExecutionContext();
        }

        [Test]
        public void CopyFromDocument()
        {
            mapper = new ReflectionScoreMapper<Sample>(typeof(Sample).GetProperty("Score"));
            sample.Score = -22f;
            queryExecutionContext.Phase = QueryExecutionPhase.ConvertResults;
            queryExecutionContext.CurrentScoreDoc = new ScoreDoc(0, 123f);

            mapper.CopyFromDocument(document, queryExecutionContext, sample);

            Assert.That(sample.Score, Is.EqualTo(queryExecutionContext.CurrentScoreDoc.Score));
        }

        [Test]
        public void CopyFromDocumentBeforeConvertPhaseDoesNothing()
        {
            mapper = new ReflectionScoreMapper<Sample>(typeof(Sample).GetProperty("Score"));
            sample.Score = -22f;
            queryExecutionContext.Phase = QueryExecutionPhase.Execute;

            mapper.CopyFromDocument(document, queryExecutionContext, sample);

            Assert.That(sample.Score, Is.EqualTo(-22f));
        }

    }
}