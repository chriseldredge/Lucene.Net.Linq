using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class FieldMappingInfoBuilderCollectionTests
    {
        private PropertyInfo info;
        private Document document;
        public IEnumerable<string> Strings { get; set; }

        [SetUp]
        public void SetUp()
        {
            info = GetType().GetProperty("Strings");
            document = new Document();
        }

        [Test]
        public void IEnumerableOfString_Build()
        {
            var mapper = CreateMapper();
            Assert.That(mapper.FieldName, Is.EqualTo(info.Name));
        }
        
        [Test]
        public void IEnumerableOfString_CopyToDocument_Null()
        {
            CreateMapper().CopyToDocument(this, document);
            Assert.That(document.GetValues("Strings"), Is.Empty);
        }

        [Test]
        public void IEnumerableOfString_CopyToDocument_Empty()
        {
            
            CreateMapper().CopyToDocument(this, document);
            Assert.That(document.GetValues("Strings"), Is.Empty);
        }

        [Test]
        public void IEnumerableOfString_CopyToDocument_Values()
        {
            Strings = new[] {"a", "b"};
            CreateMapper().CopyToDocument(this, document);
            Assert.That(document.GetValues("Strings"), Is.EqualTo(new[] {"a", "b"}));
        }

        [Test]
        public void IEnumerableOfString_CopyFromDocument_NoFieldsSetsEmpty()
        {
            Strings = new[] { "replace me" };
            CreateMapper().CopyFromDocument(document, this);
            Assert.That(Strings, Is.Empty);
        }

        private IFieldMapper<FieldMappingInfoBuilderCollectionTests> CreateMapper()
        {
            return FieldMappingInfoBuilder.Build<FieldMappingInfoBuilderCollectionTests>(info);
        }
    }
}