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

            TimeStamp = DateTime.MinValue;
            SillyTime = DateTime.MinValue;
            OptionalTimeStamp = null;
            OptionalTimeStampOffset = null;
        }

        [Field]
        public DateTime TimeStamp { get; set; }

        [Field(Format = SillyFormat)]
        public DateTime SillyTime { get; set; }

        [Field("TimeStamp")]
        public DateTime? OptionalTimeStamp { get; set; }

        [Field("TimeStamp")]
        public DateTimeOffset? OptionalTimeStampOffset { get; set; }

        [Test]
        public void DefaultDateTimeUsesSolrFormat_FromDocument([Values("TimeStamp", "OptionalTimeStamp", "OptionalTimeStampOffset")] string propertyName)
        {
            PropertyInfo prop;
            var mapper = CreateMapper(propertyName, out prop);

            var ts = DateTime.SpecifyKind(new DateTime(2013, 1, 2, 3, 40, 50), DateTimeKind.Utc);

            doc.Add(new Field("TimeStamp", "2013-01-02T03:40:50", Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, new QueryExecutionContext(), this);

            if (propertyName == "OptionalTimeStampOffset")
            {
                Assert.That(prop.GetValue(this, null), Is.EqualTo(new DateTimeOffset(ts)));
            }
            else
            {
                Assert.That(prop.GetValue(this, null), Is.EqualTo(ts));    
            }
            
        }

        [Test]
        public void DefaultDateTimeUsesSolrFormat_ToDocument([Values("TimeStamp", "OptionalTimeStamp", "OptionalTimeStampOffset")] string propertyName)
        {
            PropertyInfo prop;
            var mapper = CreateMapper(propertyName, out prop);
            var dateTime = new DateTime(2012, 4, 23, 4, 56, 27);

            if (Nullable.GetUnderlyingType(prop.PropertyType) == typeof(DateTimeOffset))
            {
                prop.SetValue(this, new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero), null);    
            }
            else
            {
                prop.SetValue(this, dateTime, null);
            }

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.Get("TimeStamp"), Is.EqualTo(dateTime.ToUniversalTime().ToString(FieldMappingInfoBuilder.DefaultDateTimeFormat)));
        }

        [Test]
        public void SpecifyFormat_FromDocument()
        {
            var mapper = CreateMapper(() => SillyTime);

            var ts = DateTime.SpecifyKind(new DateTime(2013, 1, 2, 18, 40, 50), DateTimeKind.Utc);

            doc.Add(new Field("SillyTime", ts.ToString(SillyFormat), Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, new QueryExecutionContext(), this);

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

        private IFieldMapper<FieldMappingInfoBuilderDateFormatTests> CreateMapper(string propertyName, out PropertyInfo info)
        {
            info = GetType().GetProperty(propertyName);
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderDateFormatTests>(info);
        }

    }
}