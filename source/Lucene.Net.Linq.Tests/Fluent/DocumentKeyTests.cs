using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class DocumentKeyTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void SetDocumentKey()
        {
            map.DocumentKey("entity-type").WithFixedValue("customer");

            var key = map.ToDocumentMapper().ToKey(new Sample());
            
            Assert.That(key.ToQuery().ToString(), Is.EqualTo("+entity-type:customer"));
        }

        [Test]
        public void ThrowsOnNullValue()
        {
            map.DocumentKey("type");

            TestDelegate call = () => map.ToDocumentMapper();

            Assert.That(call, Throws.InvalidOperationException);
        }
    }
}