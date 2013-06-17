using System;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class AnalyzerTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void SpecifyAnalyzer()
        {
            var analyzer = new SimpleAnalyzer();

            map.Property(x => x.Date).AnalyzeWith(analyzer);

            var mapper = GetMappingInfo("Date");

            Assert.That(mapper.Analyzer, Is.SameAs(analyzer));
        }

        [Test]
        public void SpecifyIndexMode()
        {
            map.Property(x => x.Name).IndexMode(IndexMode.AnalyzedNoNorms);

            var mapper = GetMappingInfo("Name");

            Assert.That(mapper.IndexMode, Is.EqualTo(IndexMode.AnalyzedNoNorms));
        }

        [TestFixture]
        public class FluentIndexModes : FluentDocumentMapperTestBase
        {
            [Test]
            public void Analyzed()
            {
                Test(p => p.Analyzed(), IndexMode.Analyzed);
            }

            [Test]
            public void AnalyzedNoNorms()
            {
                Test(p => p.AnalyzedNoNorms(), IndexMode.AnalyzedNoNorms);
            }

            [Test]
            public void NotAnalyzed()
            {
                Test(p => p.NotAnalyzed(), IndexMode.NotAnalyzed);
            }

            [Test]
            public void NotAnalyzedNoNorms()
            {
                Test(p => p.NotAnalyzedNoNorms(), IndexMode.NotAnalyzedNoNorms);
            }

            [Test]
            public void NotIndexed()
            {
                Test(p => p.NotIndexed(), IndexMode.NotIndexed);
            }

            protected void Test(Action<PropertyMap<Sample>> setIndexMode, IndexMode expectedIndexMode)
            {
                setIndexMode(map.Property(x => x.Name));
                var mapper = GetMappingInfo("Name");

                Assert.That(mapper.IndexMode, Is.EqualTo(expectedIndexMode));
            }
        }
    }
}