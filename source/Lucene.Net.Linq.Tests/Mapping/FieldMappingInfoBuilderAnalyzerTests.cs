using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderAnalyzerTests
    {
        public String Simple { get; set; }

        [Test]
        public void UsesExternalAnalyzerWhenProvided()
        {
            var externalAnalyzer = new StandardAnalyzer(Version.LUCENE_30);

            var mapper = (ReflectionFieldMapper<FieldMappingInfoBuilderAnalyzerTests>)FieldMappingInfoBuilder
                .Build<FieldMappingInfoBuilderAnalyzerTests>(
                    GetType().GetProperty("Simple"),
                    Version.LUCENE_30,
                    externalAnalyzer);

            Assert.That(mapper.Analyzer, Is.SameAs(externalAnalyzer));
        }

        [Test]
        public void UseLowercaseKeywordAnalyzerByDefault()
        {
            var mapper = Build("Simple");

            Assert.That(mapper.Analyzer, Is.InstanceOf<CaseInsensitiveKeywordAnalyzer>());
        }

        [Field(IndexMode.NotAnalyzed)]
        public String NotAnalyzed { get; set; }

        [Test]
        public void UseKeywordAnalyzerByDefaultWhenNotAnalyzed()
        {
            var mapper = Build("NotAnalyzed");

            Assert.That(mapper.Analyzer, Is.InstanceOf<KeywordAnalyzer>());
        }

        [Field(CaseSensitive = true)]
        public String CaseSensitive { get; set; }

        [Test]
        public void UseKeywordAnalyzerByDefaultWhenCaseSensitive()
        {
            var mapper = Build("CaseSensitive");

            Assert.That(mapper.Analyzer, Is.InstanceOf<KeywordAnalyzer>());
        }
        
        [Field(IndexMode.NotAnalyzed, Analyzer = typeof(StandardAnalyzer))]
        public String CustomAnalyzer { get; set; }
        
        [Test]
        public void UseSpecifiedAnalyzerType()
        {
            var mapper = Build("CustomAnalyzer");

            Assert.That(mapper.Analyzer, Is.InstanceOf<StandardAnalyzer>());
        }

        [Field(CaseSensitive = true, Analyzer = typeof(SimpleAnalyzer))]
        public String CustomAnalyzerDefaultCtr { get; set; }

        [Test]
        public void UseSpecifiedAnalyzerTypeWithDefaultCtr()
        {
            var mapper = Build("CustomAnalyzerDefaultCtr");

            Assert.That(mapper.Analyzer, Is.InstanceOf<SimpleAnalyzer>());
        }

        [Test]
        public void AnalyzerMustInheritFromBase()
        {
            TestDelegate call = () => FieldMappingInfoBuilder.CreateAnalyzer(typeof (object), Version.LUCENE_30);

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void AnalyzerMustHavePublicCtr()
        {
            TestDelegate call = () => FieldMappingInfoBuilder.CreateAnalyzer(typeof(Private), Version.LUCENE_30);

            Assert.That(call, Throws.InvalidOperationException);
        }

        private class Private
        {
            private Private()
            {
            }
        }

        private ReflectionFieldMapper<FieldMappingInfoBuilderAnalyzerTests> Build(string propertyName)
        {
            return (ReflectionFieldMapper<FieldMappingInfoBuilderAnalyzerTests>)FieldMappingInfoBuilder
                .Build<FieldMappingInfoBuilderAnalyzerTests>(
                    GetType()
                    .GetProperty(propertyName),
                    Version.LUCENE_30,
                    null);
        }
    }
}