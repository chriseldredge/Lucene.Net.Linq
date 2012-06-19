using System;
using System.Linq;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class ReflectionDocumentMapperTests
    {
        [Test]
        public void CtrFindsKeyFields()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>();
            Assert.That(mapper.KeyFields.Select(k => k.FieldName), Is.EquivalentTo(new[] {"Id", "Version", "Number"}));
        }

        [Test]
        public void ToKey_NullSafe()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>();
            var key = mapper.ToKey(new ReflectedDocument());
            Assert.NotNull(key);
        }

        [Test]
        public void ToKey_DifferentInstance()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>();
            var key1 = mapper.ToKey(new ReflectedDocument());
            var key2 = mapper.ToKey(new ReflectedDocument());
            Assert.That(key1, Is.Not.SameAs(key2));
        }

        [Test]
        public void ToKey_Equal()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>();
            var key1 = mapper.ToKey(new ReflectedDocument());
            var key2 = mapper.ToKey(new ReflectedDocument());
            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void ToKey_NotEqual()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>();
            var key1 = mapper.ToKey(new ReflectedDocument { Version = new Version("1.0") });
            var key2 = mapper.ToKey(new ReflectedDocument { Version = new Version("2.0") });
            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        public class ReflectedDocument
        {
            [Field(Key = true)]
            public string Id { get; set; }

            [Field(Converter = typeof(VersionConverter), Key = true)]
            public Version Version { get; set; }

            [Field]
            public string Name { get; set; }

            public string Location { get; set; }

            [NumericField(Key = true)]
            public int Number { get; set; }
        }
    }

    
}