using System;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using NUnit.Framework;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class LuceneQueryExecutorTests
    {
        private TestableLuceneQueryExecutor<Record> executor;
        private IDocumentMapper<Record> mapper;
        private Document document;
        private Record record;

        [SetUp]
        public void SetUp()
        {
            record = new Record();
            document = new Document();
            mapper = new MockRepository().StrictMock<IDocumentMapper<Record>>();
            executor = new TestableLuceneQueryExecutor<Record>(new Context(new RAMDirectory(), new object()), () => record, mapper);
        }

        [Test]
        public void GetDocumentKey_ConvertToObjectThenToKey()
        {
            var key = new DocumentKey();
            var context = new QueryExecutionContext();

            mapper.Expect(m => m.ToObject(document, context, record));
            mapper.Expect(m => m.ToKey(record)).Return(key);

            mapper.Replay();
            
            var result = executor.GetDocumentKey(document, context);

            Assert.That(result, Is.SameAs(key));
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void GetDocumentKey_GetKeyDirectlyWhenSupported()
        {
            var enhancedMapper = MockRepository.GenerateStrictMock<IDocumentMapperWithConverter>();
            executor = new TestableLuceneQueryExecutor<Record>(new Context(new RAMDirectory(), new object()), () => record, enhancedMapper);

            var key = new DocumentKey();
            var context = new QueryExecutionContext();

            enhancedMapper.Expect(m => m.ToKey(document)).Return(key);

            enhancedMapper.Replay();

            var result = executor.GetDocumentKey(document, context);

            Assert.That(result, Is.SameAs(key));
            enhancedMapper.VerifyAllExpectations();
        }

        class TestableLuceneQueryExecutor<T> : LuceneQueryExecutor<T>
        {
            public TestableLuceneQueryExecutor(Context context, Func<T> newItem, IDocumentMapper<T> mapper) : base(context, newItem, mapper)
            {
            }

            public new IDocumentKey GetDocumentKey(Document doc, IQueryExecutionContext context)
            {
                return base.GetDocumentKey(doc, context);
            }
        }

        public interface IDocumentMapperWithConverter : IDocumentMapper<Record>, IDocumentKeyConverter
        {
        }
    }
}