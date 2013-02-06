using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using NUnit.Framework;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class DocumentKeyTests
    {
        [Test]
        public void EmptyKeys_NotEqual()
        {
            var key1 = new DocumentKey();
            var key2 = new DocumentKey();

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void ToQuery()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object>
                                    {{new FakeFieldMappingInfo { FieldName = "id" }, "some value"}});

            var query = key.ToQuery();

            Assert.That(query.ToString(), Is.EqualTo("+id:some value"));
        }

        [Test]
        public void ToQuery_EscapesSpecialCharacters()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id" }, "**mykey**" } });

            var query = key.ToQuery();

            Assert.That(query.ToString(), Is.EqualTo("+id:**mykey**"));
        }

        [Test]
        public void ToQuery_ConvertsComplexTypes()
        {
            var customValue = new object();
            var mapping = MockRepository.GenerateStub<IFieldMappingInfo>();
            mapping.Expect(m => m.FieldName).Return("id");
            mapping.Expect(m => m.ConvertToQueryExpression(customValue)).Return("custom*value*as*string");
            mapping.Expect(m => m.CreateQuery("custom*value*as*string")).Return(new TermQuery(new Term("id", "custom*value*as*string")));
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { mapping, customValue } });

            var query = key.ToQuery();

            Assert.That(query.ToString(), Is.EqualTo("+id:custom*value*as*string"));
        }

        [Test]
        public void ToQuery_ThrowsOnBlankValue()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id" }, "" } });

            TestDelegate call = () => key.ToQuery();

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void HashCode_NotEqualForDifferentKeys()
        {
            var key1 = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id1" }, "**mykey**" } });
            var key2 = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id2" }, "**mykey**" } });

            Assert.That(key1.GetHashCode(), Is.Not.EqualTo(key2.GetHashCode()));
        }

        [Test]
        public void HashCode_NullSafe()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id1" }, null } });

            TestDelegate call = () => key.GetHashCode();

            Assert.That(call, Throws.Nothing);
        }

        [Test]
        public void HashCode_NotEqualForDifferentValues()
        {
            var key1 = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id1" }, "**mykey**" } });
            var key2 = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id1" }, "**my other key**" } });

            Assert.That(key1.GetHashCode(), Is.Not.EqualTo(key2.GetHashCode()));
        }
    }
}