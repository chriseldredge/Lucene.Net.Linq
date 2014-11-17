using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SessionTests : IntegrationTestBase
    {
        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            return new CaseInsensitiveKeywordAnalyzer();
        }

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });
        }

        [Test]
        public void ReplacesDirtyDocument()
        {
            var session = provider.OpenSession<SampleDocument>();
            
            using (session)
            {
                var item = (from d in session.Query() where d.Name == "a" select d).Single();
                item.Scalar = 4;
            }

            var result = (from d in provider.AsQueryable<SampleDocument>() where d.Name == "a" select d).Single();
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
        public void QueryOmitsDeletedDocument()
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session)
            {
                var before = session.Query().Select(d => d.Name).ToList();

                session.Delete(session.Query().Single(d => d.Name == "a"));

                var after = session.Query().Select(d => d.Name).ToList();

                Assert.That(after.Count, Is.EqualTo(before.Count - 1), "Should not return documents pending delete.");
            }
        }

        [Test]
        public void DeleteModifiedDocument()
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session)
            {
                var doc = session.Query().Single(d => d.Name == "b");
                doc.Alias = "new alias";

                session.Delete(doc);
            }

            Assert.That(session.Query().Count(d => d.Name == "b"), Is.EqualTo(0), "Should delete document and not re-add it.");
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

	    [Test]
	    public void AddReplacesModifiedDocument()
	    {
            var session = provider.OpenSession<SampleDocument>();

		    using (session)
		    {
				var item = (from d in session.Query() where d.Name == "a" select d).Single();

			    item.Name = "a modified name";

			    var newItem = new SampleDocument {Key = item.Key, Name = "a new a"};

				session.Add(newItem);

				session.Commit();

				var result = (from d in session.Query() where d.Key == newItem.Key select d).Single();
				Assert.That(result.Name, Is.EqualTo("a new a"));
		    }
	    }


        [Test]
        public void AddWithoutDeleteCanAllowDuplicates() 
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session) {
                
                var newItem = new SampleDocument { Key = "a1", Name = "a new a" };

                session.AddWithoutDelete(newItem);

                session.Commit();

                session.AddWithoutDelete(newItem);

                session.Commit();

                var results = (from d in session.Query() where d.Key == newItem.Key select d).ToList();
                Assert.That(results.Count(), Is.EqualTo(2));
            }
        }


        [Test]
        public void AddWithoutDeleteMixedWithAddTracksDocumentsProperly() 
        {
            var session = provider.OpenSession<SampleDocument>();

            using (session) {

                var item = (from d in session.Query() where d.Name == "a" select d).Single();

                item.Name = "a modified name";

                var newItem = new SampleDocument { Key = item.Key, Name = "a new a" };
                var newItemNoDelete = new SampleDocument { Name = "d" };

                session.Add(newItem);
                session.AddWithoutDelete(newItemNoDelete);

                session.Commit();

                var result = (from d in session.Query() where d.Key == newItem.Key select d).Single();
                var resultOfNewItem = (from d in session.Query() where d.Key == newItemNoDelete.Key select d).Single();
                Assert.That(result.Name, Is.EqualTo("a new a"));
                Assert.That(resultOfNewItem.Name, Is.EqualTo("d"));
            }
        }
				
				
    }
}