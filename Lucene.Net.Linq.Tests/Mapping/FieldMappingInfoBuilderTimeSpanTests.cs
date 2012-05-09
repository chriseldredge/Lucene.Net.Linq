using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderTimeSpanTests
    {
        private const string TimeSpanFormat = "c";
        private Document doc;

        [SetUp]
        public void SetUp()
        {
            doc = new Document();

            Elapsed = TimeSpan.MinValue;
            OptionalElapsed = null;
        }

        [Field(Format = TimeSpanFormat)]
        public TimeSpan Elapsed { get; set; }

        [Field("Elapsed", Format = TimeSpanFormat)]
        public TimeSpan? OptionalElapsed { get; set; }

        [Test]
        public void FromDocument([Values("Elapsed", "OptionalElapsed")] string propertyName)
        {
            PropertyInfo prop;
            var mapper = CreateMapper(propertyName, out prop);

            var ts = new TimeSpan(23, 19, 59, 58, 987);

            doc.Add(new Field("Elapsed", ts.ToString(TimeSpanFormat), Field.Store.YES, Field.Index.NOT_ANALYZED));

            mapper.CopyFromDocument(doc, this);

            Assert.That(prop.GetValue(this, null), Is.EqualTo(ts));    
        }

        [Test]
        public void ToDocument([Values("Elapsed", "OptionalElapsed")] string propertyName)
        {
            PropertyInfo prop;
            var mapper = CreateMapper(propertyName, out prop);
            var ts = new TimeSpan(4, 23, 4, 56, 27);

            prop.SetValue(this, ts, null);

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.Get("Elapsed"), Is.EqualTo(ts.ToString(TimeSpanFormat)));
        }
        
        private IFieldMapper<FieldMappingInfoBuilderTimeSpanTests> CreateMapper(string propertyName, out PropertyInfo info)
        {
            info = GetType().GetProperty(propertyName);
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTimeSpanTests>(info);
        }

    }
}