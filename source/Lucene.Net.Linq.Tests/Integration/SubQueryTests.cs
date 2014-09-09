using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SubQueryTests : IntegrationTestBase
    {
        [SetUp]
        public void SetUp()
        {
            AddDocument(new SampleDocument { Id = "a", Scalar = 5});
            AddDocument(new SampleDocument { Id = "b", Scalar = 1});
            AddDocument(new SampleDocument { Id = "c", Scalar = 3});
        }

        [Test]
        public void SubquerySkip()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs
                                orderby d.Id
                                select d).Skip(2)
                           select new { outer.Id })
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "c" }));
        }

        [Test]
        public void MainSkipSubquerySkip()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs
                                orderby d.Id descending
                                select d).Skip(1)
                           select new { outer.Id })
                           .Skip(1)
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void MainOrderBy()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs select d)
                           orderby outer.Id descending
                           select new { outer.Id })
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "c", "b", "a" }));
        }

        [Test]
        public void SubQueryOrderWinsOnConflictingOrder()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs
                                orderby d.Scalar
                                select d).Skip(1)
                           orderby outer.Id
                           select new { outer.Id })
                           .Skip(1)
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void AllowsConflictingOrderOnNoSkipTake()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs
                                 orderby d.Scalar
                                 select d)
                           orderby outer.Id
                           select new { outer.Id })
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "b", "c", "a" }));
        }

        [Test]
        public void AllowsSameOrderOnSubSkipTake()
        {
            var docs = provider.AsQueryable<SampleDocument>();

            var results = (from outer in
                               (from d in docs
                                orderby d.Scalar
                                orderby d.Id
                                select d).Skip(1)
                           orderby outer.Scalar
                           orderby outer.Id
                           select new { outer.Id })
                           .ToList();

            Assert.That(results.Select(d => d.Id).ToArray(), Is.EqualTo(new[] { "c", "a" }));
        }

    }
}
