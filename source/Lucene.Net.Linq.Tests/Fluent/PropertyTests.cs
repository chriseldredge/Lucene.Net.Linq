using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class PropertyTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void Defaults()
        {
            map.Property(x => x.Name);

            var info = GetMappingInfo("Name");

            Assert.That(info, Is.Not.Null);
            Assert.That(info.Store, Is.EqualTo(StoreMode.Yes));
            Assert.That(info.IndexMode, Is.EqualTo(IndexMode.Analyzed));
            Assert.That(info.Analyzer, Is.Null);
            Assert.That(info.Boost, Is.EqualTo(1.0f));
            Assert.That(info.CaseSensitive, Is.False);
            Assert.That(info.TermVector, Is.EqualTo(TermVectorMode.No));
            Assert.That(info.Converter, Is.Null);
            Assert.That(info.FieldName, Is.EqualTo("Name"));
            Assert.That(info.PropertyName, Is.EqualTo("Name"));
            Assert.That(info.PropertyInfo, Is.Not.Null);
        }

        [Test]
        public void FieldName()
        {
            map.Property(x => x.Name).ToField("_name");

            var info = GetMappingInfo("Name");

            Assert.That(info.FieldName, Is.EqualTo("_name"));
        }

        [Test]
        public void Key()
        {
            map.Key(x => x.Id).ToField("_id");
        }

        [Test]
        public void Boost()
        {
            map.Property(x => x.Name).BoostBy(5f);

            var info = GetMappingInfo("Name");

            Assert.That(info.Boost, Is.EqualTo(5f));
        }

        [Test]
        public void CaseSensitive()
        {
            map.Property(x => x.Name).CaseSensitive();

            var info = GetMappingInfo("Name");

            Assert.That(info.CaseSensitive, Is.True);
        }
    }
}
