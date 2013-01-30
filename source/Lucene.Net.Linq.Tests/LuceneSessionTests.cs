using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using NUnit.Framework;
using Rhino.Mocks;
using Version = Lucene.Net.Util.Version;

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
            analyzer = new LowercaseKeywordAnalyzer();

            context = MockRepository.GenerateStub<Context>(null, analyzer, Version.LUCENE_29, writer, new object());

            session = new LuceneSession<Record>(mapper, context, null);

            mapper.Expect(m => m.ToKey(Arg<Record>.Is.NotNull))
                .WhenCalled(mi => mi.ReturnValue = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "Id"}, ((Record)mi.Arguments[0]).Id } }))
                .Repeat.Any();
        }

        [Test]
        public void AddWithSameKeyReplaces()
        {
            var r1 = new Record { Id = "11", Name = "A" };
            var r2 = new Record { Id = "11", Name = "B" };

            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(r2), Arg<Document>.Is.NotNull));

            session.Add(r1, r2);
            var pendingAdditions = session.ConvertPendingAdditions();

            Verify();
            
            Assert.That(pendingAdditions.Count(), Is.EqualTo(1));
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
        public void Commit_Add_DeletesKey()
        {
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "Id" }, 1 } });

            var record = new Record {Id = "1"};

            session.Add(record);

            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(record), Arg<Document>.Is.NotNull));
            writer.Expect(w => w.DeleteDocuments(new[] {key.ToQuery(context.Analyzer, context.Version)}));
            writer.Expect(w => w.AddDocument(Arg<Document>.Is.NotNull));
            writer.Expect(w => w.Commit());

            session.Commit();

            Verify();

            Assert.That(session.ConvertPendingAdditions, Is.Empty, "Commit should clear pending deletions.");
        }

        [Test]
        public void Commit_Add_ConvertsDocumentAndKeyLate()
        {
            var record = new Record();
            var key = new DocumentKey(new Dictionary<IFieldMappingInfo, object> { { new FakeFieldMappingInfo { FieldName = "Id" }, "biully" } });
            var deleteQuery = key.ToQuery(context.Analyzer, Version.LUCENE_29);

            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(record), Arg<Document>.Is.NotNull));
            writer.Expect(w => w.DeleteDocuments(new[] { deleteQuery }));
            writer.Expect(w => w.AddDocument(Arg<Document>.Is.NotNull));//Matches(doc => doc.GetValues("Name")[0] == "a name")));
            writer.Expect(w => w.Commit());
            
            session.Add(record);

            record.Id = "biully";
            record.Name = "a name";

            session.Commit();

            Verify();

            Assert.That(session.ConvertPendingAdditions, Is.Empty, "Commit should clear pending deletions.");
        }

        [Test]
        public void Commit_ReloadsSearcher()
        {
            session.DeleteAll();
            writer.Expect(w => w.DeleteAll());
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
            
            session.Add(r1);
            session.DeleteAll();
            
            Assert.That(session.ConvertPendingAdditions, Is.Empty, "Additions");

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
        public void Delete_RemovesFromPendingAdditions()
        {
            var r1 = new Record { Id = "12" };
            session.Add(r1);
            session.Delete(r1);

            Assert.That(session.Additions, Is.Empty);
            Assert.That(session.Deletions.Single().ToString(), Is.EqualTo("+Id:12"));
        }

        [Test]
        public void Delete_MarkedForDeletion()
        {
            var r1 = new Record { Id = "12" };
            
            session.Delete(r1);

            Assert.That(session.DocumentTracker.IsMarkedForDeletion(r1), Is.True, "IsMarkedForDeletion");
        }

        [Test]
        public void Delete_MarkedForDeletion_ClearedOnRollback()
        {
            var r1 = new Record { Id = "12" };

            session.Delete(r1);

            session.Rollback();

            Assert.That(session.DocumentTracker.IsMarkedForDeletion(r1), Is.False, "IsMarkedForDeletion");
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
            mapper.BackToRecord(BackToRecordOptions.All);
            mapper.Expect(m => m.ToKey(Arg<Record>.Is.NotNull)).Return(new DocumentKey());
            mapper.Replay();

            var r1 = new Record { Id = "12" };

            TestDelegate call = () => session.Delete(r1);

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void Query_Attaches()
        {
            var records = new Record[0].AsQueryable();
            var provider = MockRepository.GenerateStrictMock<IQueryProvider>();
            var queryable = MockRepository.GenerateStrictMock<IQueryable<Record>>();
            queryable.Expect(q => q.Provider).Return(provider);
            queryable.Expect(q => q.Expression).Return(Expression.Constant(records));
            provider.Expect(p => p.CreateQuery<Record>(Arg<Expression>.Is.NotNull)).Return(records);
            session = new LuceneSession<Record>(mapper, context, queryable);

            session.Query();

            queryable.VerifyAllExpectations();
            provider.VerifyAllExpectations();
        }

        [Test]
        public void PendingChanges_DirtyDocuments()
        {
            var record = new Record();
            var copy = new Record();
            mapper.Expect(m => m.Equals(record, copy)).Return(false);
            mapper.Expect(m => m.ToDocument(Arg<Record>.Is.Same(record), Arg<Document>.Is.NotNull));
            session.DocumentTracker.TrackDocument(record, copy);
            record.Id = "1";

            session.StageModifiedDocuments();

            Assert.That(session.PendingChanges, Is.True, "Should detect modified document.");
        }

        [Test]
        public void Dispose_Commits()
        {
            writer.Expect(w => w.DeleteAll());
            writer.Expect(w => w.Commit());

            session.DeleteAll();
            session.Dispose();
        }

        [Test]
        public void Commit_RollbackException_ThrowsAggregateException()
        {
            var ex1 = new Exception("ex1");
            var ex2 = new Exception("ex2");
            writer.Expect(w => w.DeleteAll()).Throw(ex1);
            writer.Expect(w => w.Rollback()).Throw(ex2);

            session.DeleteAll();

            var thrown = Assert.Throws<AggregateException>(session.Commit);
            Assert.That(thrown.InnerExceptions, Is.EquivalentTo(new[] {ex1, ex2}));
        }

        private void Verify()
        {
            mapper.VerifyAllExpectations();
            writer.VerifyAllExpectations();
        }
    }
}