using System.IO;
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
            var analyzer = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            analyzer.AddAnalyzer("Path", new LowercaseKeywordAnalyzer());
            return analyzer;
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
            AddDocument(new PathDocument { Path = "AA" });
            AddDocument(new PathDocument { Path = "AB" });

            var documents = provider.AsQueryable<PathDocument>();

            var result = (from doc in documents where doc.Path == "A*".AllowSpecialCharacters() select doc).ToList();
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

        [Test]
        public void AllowSpecialCharacters_QuotedPhrase()
        {
            AddDocument(new PathDocument { Name = "Apple Banana Cucumber" });
            AddDocument(new PathDocument { Name = "Banana Cucumber Apple" });

            var documents = provider.AsQueryable<PathDocument>();

            var result = (from doc in documents where (doc.Name == "\"Apple Banana\"").AllowSpecialCharacters() select doc).ToList();
            Assert.That(result.Count(), Is.EqualTo(1));
        }

        public class PathDocument
        {
            public string Path { get; set; }
            public string Name { get; set; }
        }
    }
}