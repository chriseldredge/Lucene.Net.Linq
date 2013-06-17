using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class ScoreTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void CaptureScore()
        {
            map.Score(x => x.Score);

            var mapper = GetMappingInfo<ReflectionScoreMapper<Sample>>("Score");

            Assert.That(mapper, Is.Not.Null);
        }
    }
}