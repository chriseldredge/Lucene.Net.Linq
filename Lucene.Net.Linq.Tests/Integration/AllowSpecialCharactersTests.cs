using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class AllowSpecialCharactersTests : IntegrationTestBase
    {
        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            return new KeywordAnalyzer();
        }

        [Test]
        public void EscapeSpecialChars()
        {
            const string path = @"*";
            AddDocument(new PathDocument { Path = path });
            AddDocument(new PathDocument { Path = "NotWildcard" });

            var documents = provider.AsQueryable<PathDocument>();

            var result = (from doc in documents where doc.Path == path select doc).ToList();
            Assert.That(result.Single().Path, Is.EqualTo(path));
        }

        [Test]
        public void AllowSpecialCharacters()
        {
            AddDocument(new PathDocument { Path = "A" });
            AddDocument(new PathDocument { Path = "B" });

            var documents = provider.AsQueryable<PathDocument>();

            var result = (from doc in documents where doc.Path == "*".AllowSpecialCharacters() select doc).ToList();
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void AllowSpecialCharacters_MultipleFields()
        {
            AddDocument(new PathDocument { Path = "A" });
            AddDocument(new PathDocument { Name = "B" });

            var documents = provider.AsQueryable<PathDocument>();

            var result = (from doc in documents where (doc.Path == "*" || doc.Name == "*").AllowSpecialCharacters() select doc).ToList();
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        public class PathDocument
        {
            [Field(IndexMode.NotAnalyzed)]
            public string Path { get; set; }

            [Field(IndexMode.NotAnalyzed)]
            public string Name { get; set; }
        }
    }
}