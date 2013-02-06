using System;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderTests
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public ComplexType ComplexProperty { get; set; }

        private PropertyInfo stringPropertyInfo;

        [SetUp]
        public void SetUp()
        {
            stringPropertyInfo = GetType().GetProperty("StringProperty");
        }

        [Test]
        public void FieldIsPropertyName()
        {
            var context = FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(stringPropertyInfo);
            Assert.That(context.FieldName, Is.EqualTo(stringPropertyInfo.Name));
        }

        [Test]
        public void RetainsPropertyInfo()
        {
            var context = (ReflectionFieldMapper<FieldMappingInfoBuilderTests>)FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(stringPropertyInfo);
            Assert.That(context.PropertyName, Is.EqualTo(stringPropertyInfo.Name));
            Assert.That(context.PropertyInfo, Is.EqualTo(stringPropertyInfo));
        }

        [Test]
        public void NoConverterForStrings()
        {
            var context = (ReflectionFieldMapper<FieldMappingInfoBuilderTests>)FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(stringPropertyInfo);
            Assert.That(context.Converter, Is.Null, "No converter should be necessary for typeof(string)");
        }

        [Test]
        public void BuildFromIntProperty()
        {
            var propertyInfo = GetType().GetProperty("IntProperty");
            var context = (ReflectionFieldMapper<FieldMappingInfoBuilderTests>)FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(propertyInfo);

            Assert.That(context.Converter, Is.EqualTo(TypeDescriptor.GetConverter(typeof(int))));
        }

        [Test]
        public void BuildThrowsOnUnconvertableType()
        {
            var propertyInfo = GetType().GetProperty("ComplexProperty");
            TestDelegate call = () => FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(propertyInfo);

            Assert.That(call, Throws.Exception.InstanceOf<NotSupportedException>());
        }

        [Field(Converter = typeof(ComplexTypeConverter))]
        public ComplexType ComplexPropertyWithConverter { get; set; }

        [Test]
        public void BuildFromComplexTypeWithCustomConverter()
        {
            var propertyInfo = GetType().GetProperty("ComplexPropertyWithConverter");
            var context = (ReflectionFieldMapper<FieldMappingInfoBuilderTests>)FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(propertyInfo);

            Assert.That(context.Converter, Is.InstanceOf<ComplexTypeConverter>());
        }

        [Field("ugly_lucene_field_name")]
        public string FriendlyName { get; set; }

        [Test]
        public void OverrideFieldName()
        {
            var propertyInfo = GetType().GetProperty("FriendlyName");
            var context = FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderTests>(propertyInfo);

            Assert.That(context.FieldName, Is.EqualTo("ugly_lucene_field_name"));
        }

        [Test]
        public void CaseSensitive_WhenPropertySet()
        {
            var flag = FieldMappingInfoBuilder.GetCaseSensitivity(new FieldAttribute {CaseSensitive = true});
            Assert.That(flag, Is.True);
        }

        [Test]
        public void CaseSensitive_IndexMode_NotAnalayzed()
        {
            var flag = FieldMappingInfoBuilder.GetCaseSensitivity(new FieldAttribute(IndexMode.NotAnalyzed) { CaseSensitive = false });
            Assert.That(flag, Is.True);
        }

        [Test]
        public void CaseSensitive_IndexMode_NotAnalyzedNoNorms()
        {
            var flag = FieldMappingInfoBuilder.GetCaseSensitivity(new FieldAttribute(IndexMode.NotAnalyzedNoNorms) { CaseSensitive = false });
            Assert.That(flag, Is.True);
        }

        [Test]
        public void CaseSensitive_False()
        {
            var flag = FieldMappingInfoBuilder.GetCaseSensitivity(new FieldAttribute());
            Assert.That(flag, Is.False);
        }

        [Test]
        public void CaseSensitive_NullMetadata_False()
        {
            var flag = FieldMappingInfoBuilder.GetCaseSensitivity(null);
            Assert.That(flag, Is.False);
        }

        public class ComplexType
        {
        }

        public class ComplexTypeConverter : TypeConverter
        {
        }

    }
}
