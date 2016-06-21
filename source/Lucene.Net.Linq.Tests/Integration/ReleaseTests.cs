using System;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class ReleaseTests
    {
        public class Document
        {
            public int Id { get; set; }
        }

        [Test]
        public void CommitInSessionAndDisposeProviderReleasesFiles()
        {
            var dir = "index." + DateTime.Now.Ticks;
            System.IO.Directory.CreateDirectory(dir);
            var provider = new LuceneDataProvider(FSDirectory.Open(dir), Version.LUCENE_30);
            using (provider)
            {
                using (var session = provider.OpenSession<Document>())
                {
                    session.Add(new Document { Id = 1 });

                    session.Commit();

                    session.Add(new Document { Id = 2 });
                }
            }

            TestDelegate call = () => System.IO.Directory.Delete(dir, true);
            Assert.That(call, Throws.Nothing);
        }
    }
}
