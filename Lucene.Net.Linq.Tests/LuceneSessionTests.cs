using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using NUnit.Framework;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class LuceneSessionTests
    {
        private LuceneSession<Record> session;
        private IDocumentMapper<Record> mapper;
        private IIndexWriter writer;
        private Context context;

        [SetUp]
        public void SetUp()
        {
            mapper = MockRepository.GenerateStrictMock<IDocumentMapper<Record>>();
            writer = MockRepository.GenerateStrictMock<IIndexWriter>();

            context = MockRepository.GenerateStub<Context>(null, null, null, writer, new object());

            session = new LuceneSession<Record>(mapper, context);
        }

        [Test]
        public void DeleteAll()
        {
            session.DeleteAll();

            Assert.That(session.DeleteAllFlag, Is.True, "DeleteAllFlag");

            Verify();
        }

        [Test]
        public void Commit_DeleteAll()
        {
            session.DeleteAll();

            writer.Expect(w => w.DeleteAll());
            writer.Expect(w => w.Commit());

            session.Commit();

            Assert.That(session.DeleteAllFlag, Is.False, "Commit should reset flag.");

            Verify();
        }

        [Test]
        public void Commit_Delete()
        {
            var q1 = new TermQuery(new Term("field1", "value1"));
            var q2 = new TermQuery(new Term("field1", "value2"));

            session.Delete(q1, q2);

            writer.Expect(w => w.DeleteDocuments(new[] {q1, q2}));
            writer.Expect(w => w.Commit());

            session.Commit();

            Assert.That(session.Deletions, Is.Empty, "Commit should clear pending deletions.");

            Verify();
        }

        [Test]
        public void Commit_Add()
        {
            var doc1 = new Document();
            var doc2 = new Document();

            session.Add(doc1, doc2);

            writer.Expect(w => w.AddDocument(doc1));
            writer.Expect(w => w.AddDocument(doc2));
            writer.Expect(w => w.Commit());

            session.Commit();

            Verify();

            Assert.That(session.Additions, Is.Empty, "Commit should clear pending deletions.");
        }

        [Test]
        public void Commit_ReloadsSearcher()
        {
            var doc1 = new Document();

            session.Add(doc1);

            writer.Expect(w => w.AddDocument(doc1));
            writer.Expect(w => w.Commit());

            session.Commit();

            context.AssertWasCalled(c => c.Reload());
        }

        [Test]
        public void Commit_NoPendingChanges()
        {
            session.Commit();

            Verify();
        }

        [Test]
        public void Add()
        {
            var r1 = new Record();
            var r2 = new Record();

            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r1), Arg<Document>.Is.NotNull));
            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r2), Arg<Document>.Is.NotNull));

            session.Add(r1, r2);

            Verify();

            Assert.That(session.Additions.Count(), Is.EqualTo(2));
            Assert.That(session.Additions.First(), Is.Not.SameAs(session.Additions.Skip(1).First()));
        }

        private void Verify()
        {
            mapper.VerifyAllExpectations();
            writer.VerifyAllExpectations();
        }
    }
}