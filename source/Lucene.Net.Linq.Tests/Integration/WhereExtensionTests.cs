using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class WhereExtensionTests : IntegrationTestBase
    {
        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            return new KeywordAnalyzer();
        }

        [Test]
        public void Where()
        {
            AddDocument(new SampleDocument { Name = "Documents Bill", Id = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "Bills Document", Id = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = documents.Where(new TermQuery(new Term("Name", "Bills Document")));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }
    }
}