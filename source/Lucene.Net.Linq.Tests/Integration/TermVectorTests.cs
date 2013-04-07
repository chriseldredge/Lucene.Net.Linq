using System.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class TermVectorTests : IntegrationTestBase
    {
        [Test]
        public void GetTermVectors()
        {
            AddDocument(new TermVectorDoc { Content = "car truck boat train trucks", NoTerms = "no term analysis for this field."});

            var mapper = new TermFreqVectorDocumentMapper<TermVectorDoc>(Version.LUCENE_30);

            var doc = provider.AsQueryable(mapper).Single();

            var termFreqVectors = mapper[doc];
            Assert.That(termFreqVectors, Is.Not.Null);
            Assert.That(termFreqVectors.Length, Is.EqualTo(1));

            var termFreqVector = termFreqVectors[0];

            Assert.That(termFreqVector, Is.Not.Null);
            Assert.That(termFreqVector.Field, Is.EqualTo("Content"));
            Assert.That(termFreqVector.GetTerms(), Is.EqualTo(new[] {"boat", "car", "train", "truck"}));
            Assert.That(termFreqVector.GetTermFrequencies(), Is.EqualTo(new[] {1, 1, 1, 2}));
        }

        public class TermVectorDoc
        {
            [Field(TermVector = TermVectorMode.Yes)]
            public string Content { get; set; }

            [Field(TermVector = TermVectorMode.No)]
            public string NoTerms { get; set; }
        }
    }
}