using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderNumericDateTimeTests
    {
        private PropertyInfo info;

        [NumericField]
        public DateTime TimeStamp { get; set; }

        [SetUp]
        public void SetUp()
        {
            info = GetType().GetProperty("TimeStamp");
        }

        [Test]
        public void CopyToDocument()
        {
            TimeStamp = new DateTime(2012, 4, 23);

            var mapper = CreateMapper();

            var doc = new Document();

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.GetFieldable("TimeStamp").TokenStreamValue().ToString(), Is.EqualTo("(numeric,valSize=64,precisionStep=4)"));
            Assert.That(doc.GetFieldable("TimeStamp").StringValue(), Is.EqualTo(TimeStamp.ToUniversalTime().Ticks.ToString()));
        }

        [Test]
        public void CopyFromDocument()
        {
            var mapper = CreateMapper();

            var doc = new Document();

            var ts = new DateTime(2013, 1, 1).ToUniversalTime();
            doc.Add(new Field("TimeStamp", ts.ToUniversalTime().Ticks.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, this);

            Assert.That(TimeStamp, Is.EqualTo(ts));
        }

        private IFieldMapper<FieldMappingInfoBuilderNumericDateTimeTests> CreateMapper()
        {
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderNumericDateTimeTests>(info);
        }

    }
}