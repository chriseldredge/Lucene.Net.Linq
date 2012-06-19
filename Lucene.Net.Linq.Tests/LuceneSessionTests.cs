using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class LuceneSessionTests
    {
        private LuceneSession<Record> session;
        private IDocumentMapper<Record> mapper;
        private Analyzer analyzer;
        private IIndexWriter writer;
        private Context context;

        [SetUp]
        public void SetUp()
        {
            mapper = MockRepository.GenerateStrictMock<IDocumentMapper<Record>>();
            writer = MockRepository.GenerateStrictMock<IIndexWriter>();
            analyzer = new StandardAnalyzer(Version.LUCENE_29);

            context = MockRepository.GenerateStub<Context>(null, analyzer, Version.LUCENE_29, writer, new object());

            session = new LuceneSession<Record>(mapper, context);

            mapper.Expect(m => m.ToKey(Arg<Record>.Is.NotNull))
                .WhenCalled(mi => mi.ReturnValue = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "Id"}, ((Record)mi.Arguments[0]).Id } }))
                .Repeat.Any();
        }

        [Test]
        public void AddWithSameKeyReplaces()
        {
            var r1 = new Record { Id = "11", Name = "A" };
            var r2 = new Record { Id = "11", Name = "B" };

            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r1), Arg<Document>.Is.NotNull));
            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r2), Arg<Document>.Is.NotNull));

            session.Add(r1, r2);

            Verify();

            Assert.That(session.Additions.Count(), Is.EqualTo(1));
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

            var r1 = new Record { Id = "12" };

            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> {{ new FakeFieldMappingInfo { FieldName = "Id"}, 12}});

            session.Delete(r1);

            session.Delete(q1, q2);

            writer.Expect(w => w.DeleteDocuments(new[] {q1, q2, key.ToQuery(context.Analyzer, context.Version)}));
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

            session.Add(new DocumentKey(), doc1);
            session.Add(new DocumentKey(), doc2);

            writer.Expect(w => w.AddDocument(doc1));
            writer.Expect(w => w.AddDocument(doc2));
            writer.Expect(w => w.Commit());

            session.Commit();

            Verify();

            Assert.That(session.Additions, Is.Empty, "Commit should clear pending deletions.");
        }

        [Test]
        public void Commit_Add_DeletesKey()
        {
            var doc1 = new Document();
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "Id" }, 1 } });

            session.Add(key, doc1);

            writer.Expect(w => w.DeleteDocuments(new[] {key.ToQuery(context.Analyzer, context.Version)}));
            writer.Expect(w => w.AddDocument(doc1));
            writer.Expect(w => w.Commit());

            session.Commit();

            Verify();

            Assert.That(session.Additions, Is.Empty, "Commit should clear pending deletions.");
        }
        [Test]
        public void Commit_ReloadsSearcher()
        {
            var doc1 = new Document();

            session.Add(new DocumentKey(), doc1);

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
        public void DeleteAll()
        {
            session.DeleteAll();

            Assert.That(session.DeleteAllFlag, Is.True, "DeleteAllFlag");

            Verify();
        }

        [Test]
        public void DeleteAllClearsPendingAdditions()
        {
            var r1 = new Record();
            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r1), Arg<Document>.Is.NotNull));
            
            session.Add(r1);
            session.DeleteAll();

            Assert.That(session.Additions, Is.Empty, "Additions");

            Verify();
        }

        [Test]
        public void Delete()
        {
            var r1 = new Record { Id = "12" };

            session.Delete(r1);

            Assert.That(session.Deletions.Single().ToString(), Is.EqualTo("+Id:12"));
        }

        [Test]
        public void Delete_SetsPendingChangesFlag()
        {
            var r1 = new Record { Id = "12" };

            session.Delete(r1);

            Assert.That(session.PendingChanges, Is.True, "PendingChanges");
        }


        [Test]
        public void Delete_ThrowsOnEmptyKey()
        {
            mapper.BackToRecord(BackToRecordOptions.Expectations);
            mapper.Expect(m => m.ToKey(Arg<Record>.Is.NotNull)).Return(new DocumentKey());

            var r1 = new Record { Id = "12" };

            TestDelegate call = () => session.Delete(r1);

            Assert.That(call, Throws.InvalidOperationException);
        }

        private void Verify()
        {
            mapper.VerifyAllExpectations();
            writer.VerifyAllExpectations();
        }
    }
}