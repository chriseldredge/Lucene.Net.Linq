using System;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderNumericDateTimeOffsetTests
    {
        private PropertyInfo info;

        [NumericField]
        public DateTimeOffset? TimeStampOffset { get; set; }

        [SetUp]
        public void SetUp()
        {
            info = GetType().GetProperty("TimeStampOffset");
        }

        [Test]
        public void CopyToDocument()
        {
            TimeStampOffset = new DateTimeOffset(new DateTime(2012, 4, 23), TimeSpan.Zero);

            var mapper = CreateMapper();

            var doc = new Document();

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.GetFieldable("TimeStampOffset").TokenStreamValue().ToString(), Is.EqualTo("(numeric,valSize=64,precisionStep=4)"));
            Assert.That(doc.GetFieldable("TimeStampOffset").StringValue(), Is.EqualTo(TimeStampOffset.Value.UtcTicks.ToString()));
        }

        [Test]
        public void CopyFromDocument()
        {
            var mapper = CreateMapper();

            var doc = new Document();

            var ts = new DateTimeOffset(new DateTime(2013, 1, 1));
            doc.Add(new Field("TimeStampOffset", ts.ToUniversalTime().Ticks.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, this);

            Assert.That(TimeStampOffset, Is.EqualTo(ts));
        }

        private IFieldMapper<FieldMappingInfoBuilderNumericDateTimeOffsetTests> CreateMapper()
        {
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderNumericDateTimeOffsetTests>(info);
        }

    }
}