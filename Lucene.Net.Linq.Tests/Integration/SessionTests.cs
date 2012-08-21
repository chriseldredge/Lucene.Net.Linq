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

        [Test]
        public void QueryReturnsDirtyDocument()
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();
                var dupe = (from d in session.Query() where d.Name == "a" select d).Single();

                Assert.That(dupe, Is.SameAs(item), "Should return same instance of tracked document within session.");
            }
        }

        [Test]
        public void ModifiedKeyDeletesByPreviousKey()
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();

                item.Key = item.Key + "2";

                session.Commit();

                var results = from d in session.Query() where d.Name == "a" select d;
                Assert.That(results.Count(), Is.EqualTo(1));
                Assert.That(results.Single().Key, Is.EqualTo(item.Key));
            }
        }

        [Test]
        public void RollbackThenCommitDiscardsTrackedDocumentModifications()
        {
            var session = provider.OpenSession<SampleDocument>();
            int originalScalar;

            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();

                originalScalar = item.Scalar;
                item.Scalar = originalScalar + 1;

                session.Rollback();
            }

            Assert.That(provider.AsQueryable<SampleDocument>().Single(doc => doc.Name == "a").Scalar, Is.EqualTo(originalScalar));
        }

        [Test]
        public void RollbackThenQueryDiscardsTrackedDocumentModifications()
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();

                item.Scalar++;

                session.Rollback();

                var after = (from d in session.Query() where d.Name == "a" select d).Single();

                Assert.That(after, Is.Not.SameAs(item), "Should discard tracked instance after rollback");
            }
        }
    }
}