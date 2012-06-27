using System;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SessionTests : IntegrationTestBase
    {
        protected override Analysis.Analyzer GetAnalyzer(Net.Util.Version version)
        {
            return new LowercaseKeywordAnalyzer();
        }

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });
        }

        [Test]
        public void Query()
        {
            var session = provider.OpenSession<SampleDocument>();
            
            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();
                item.Scalar = 4;
            }

            var result = (from d in session.Query() where d.Name == "a" select d).Single();
            Assert.That(result.Scalar, Is.EqualTo(4));
        }
    }
}