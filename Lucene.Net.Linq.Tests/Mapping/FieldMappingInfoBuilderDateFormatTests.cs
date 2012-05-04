using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderDateFormatTests
    {
        private const string SillyFormat = "'silly' tt mm_yyyy ss:MM dd hh 'time'";
        private Document doc;

        [SetUp]
        public void SetUp()
        {
            doc = new Document();
        }

        [Field]
        public DateTime TimeStamp { get; set; }

        [Field(Format = SillyFormat)]
        public DateTime SillyTime { get; set; }

        [Test]
        public void DefaultDateTimeUsesSolrFormat_FromDocument()
        {
            var mapper = CreateMapper(() => TimeStamp);

            var ts = DateTime.SpecifyKind(new DateTime(2013, 1, 2, 3, 40, 50), DateTimeKind.Utc);

            doc.Add(new Field("TimeStamp", "2013-01-02T03:40:50", Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, this);

            Assert.That(TimeStamp.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(TimeStamp, Is.EqualTo(ts));
        }

        [Test]
        public void DefaultDateTimeUsesSolrFormat_ToDocument()
        {
            TimeStamp = new DateTime(2012, 4, 23, 4, 56, 27);

            var mapper = CreateMapper(() => TimeStamp);

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.Get("TimeStamp"), Is.EqualTo(TimeStamp.ToUniversalTime().ToString(FieldMappingInfoBuilder.DefaultDateTimeFormat)));
        }

        [Test]
        public void SpecifyFormat_FromDocument()
        {
            var mapper = CreateMapper(() => SillyTime);

            var ts = DateTime.SpecifyKind(new DateTime(2013, 1, 2, 18, 40, 50), DateTimeKind.Utc);

            doc.Add(new Field("SillyTime", ts.ToString(SillyFormat), Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, this);

            Assert.That(SillyTime.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(SillyTime, Is.EqualTo(ts));
        }

        [Test]
        public void SpecifyFormat_ToDocument()
        {
            SillyTime = new DateTime(2012, 4, 23, 4, 56, 27);

            var mapper = CreateMapper(() => SillyTime);

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.Get("SillyTime"), Is.EqualTo(SillyTime.ToUniversalTime().ToString(SillyFormat)));
        }

        private IFieldMapper<FieldMappingInfoBuilderDateFormatTests> CreateMapper<T>(Expression<Func<T>> expression)
        {
            var info = ((MemberExpression) expression.Body).Member;
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderDateFormatTests>((PropertyInfo)info);
        }

    }
}