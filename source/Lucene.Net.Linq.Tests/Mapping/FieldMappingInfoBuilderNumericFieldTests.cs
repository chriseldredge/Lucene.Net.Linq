using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderNumericFieldTests
    {
        private PropertyInfo info;

        [NumericField(Converter = typeof(SampleValueTypeConverter))]
        public SampleValueType CustomValueType { get; set; }

        [SetUp]
        public void SetUp()
        {
            info = GetType().GetProperty("CustomValueType");
        }

        [Test]
        public void CopyToDocument()
        {
            CustomValueType = new SampleValueType {TheValue = 1.34d};
            var mapper = CreateMapper();

            var doc = new Document();

            mapper.CopyToDocument(this, doc);

            Assert.That(doc.GetFieldable("CustomValueType").TokenStreamValue.ToString(), Is.EqualTo("(numeric,valSize=64,precisionStep=4)"));
            Assert.That(doc.GetFieldable("CustomValueType").StringValue, Is.EqualTo(CustomValueType.TheValue.ToString()));
        }

        [Test]
        public void CopyFromDocument()
        {
			var value = 2.68d;
            CustomValueType = null;
            var mapper = CreateMapper();

            var doc = new Document();
            doc.Add(new Field("CustomValueType", value.ToString(), Field.Store.YES, Field.Index.NO));

            mapper.CopyFromDocument(doc, new QueryExecutionContext(), this);

            Assert.That(CustomValueType, Is.Not.Null);
	        
	        Assert.That(CustomValueType.TheValue, Is.EqualTo(value));
        }

        private IFieldMapper<FieldMappingInfoBuilderNumericFieldTests> CreateMapper()
        {
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderNumericFieldTests>(info);
        }

        public class SampleValueType
        {
            public double TheValue { get; set; }
        }

        public class SampleValueTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof (double);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                return new SampleValueType {TheValue = (double) value};
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof (double);
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                return ((SampleValueType) value).TheValue;
            }
        }
    }
}