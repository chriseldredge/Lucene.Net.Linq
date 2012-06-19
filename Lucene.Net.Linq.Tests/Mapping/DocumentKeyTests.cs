using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Util;
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
                                    {{new FakeFieldMappingInfo { FieldName = "id" }, "AnalyzeThis"}});

            var query = key.ToQuery(new LowercaseKeywordAnalyzer(), Version.LUCENE_29);

            Assert.That(query.ToString(), Is.EqualTo("+id:analyzethis"));
        }

        [Test]
        public void ToQuery_EscapesSpecialCharacters()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id" }, "**mykey**" } });

            var query = key.ToQuery(new LowercaseKeywordAnalyzer(), Version.LUCENE_29);

            Assert.That(query.ToString(), Is.EqualTo("+id:**mykey**"));
        }

        [Test]
        public void ToQuery_ConvertsComplexTypes()
        {
            var customValue = new object();
            var mapping = MockRepository.GenerateStub<IFieldMappingInfo>();
            mapping.Expect(m => m.FieldName).Return("id");
            mapping.Expect(m => m.ConvertToQueryExpression(customValue)).Return("custom*value*as*string");
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { mapping, customValue } });

            var query = key.ToQuery(new LowercaseKeywordAnalyzer(), Version.LUCENE_29);

            Assert.That(query.ToString(), Is.EqualTo("+id:custom*value*as*string"));
        }

        [Test]
        public void ToQuery_ThrowsOnBlankValue()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "id" }, "" } });

            TestDelegate call = () => key.ToQuery(new LowercaseKeywordAnalyzer(), Version.LUCENE_29);

            Assert.That(call, Throws.InvalidOperationException);
        }
    }
}