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
        private QueryExecutionContext context;

        [SetUp]
        public void SetUp()
        {
            record = new Record();
            document = new Document();
            mapper = new MockRepository().StrictMock<IDocumentMapper<Record>>();
            executor = new TestableLuceneQueryExecutor<Record>(new Context(new RAMDirectory(), new object()), _ => record, mapper);
            context = new QueryExecutionContext();
        }

        [Test]
        public void ConvertDocument()
        {
            var capturedKey = (IDocumentKey) null;
            var record = new Record();
            ObjectLookup<Record> lookup = k => { capturedKey = k; return record; };

            var enhancedMapper = MockRepository.GenerateStrictMock<IDocumentMapperWithConverter>();
            executor = new TestableLuceneQueryExecutor<Record>(new Context(new RAMDirectory(), new object()), lookup, enhancedMapper);

            var key = new DocumentKey();
            enhancedMapper.Expect(m => m.ToKey(document)).Return(key);
            enhancedMapper.Expect(m => m.ToObject(document, context, record));

            var result = executor.ConvertDocument(document, context);

            Assert.That(capturedKey, Is.SameAs(key), "Captured Key");
            Assert.That(result, Is.SameAs(record), "Record");

            enhancedMapper.VerifyAllExpectations();
        }

        [Test]
        public void GetDocumentKey_ConvertToObjectThenToKey()
        {
            var key = new DocumentKey();

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
            executor = new TestableLuceneQueryExecutor<Record>(new Context(new RAMDirectory(), new object()), _ => record, enhancedMapper);

            var key = new DocumentKey();

            enhancedMapper.Expect(m => m.ToKey(document)).Return(key);

            enhancedMapper.Replay();

            var result = executor.GetDocumentKey(document, context);

            Assert.That(result, Is.SameAs(key));
            enhancedMapper.VerifyAllExpectations();
        }

        class TestableLuceneQueryExecutor<T> : LuceneQueryExecutor<T>
        {
            public TestableLuceneQueryExecutor(Context context, ObjectLookup<T> newItem, IDocumentMapper<T> mapper) : base(context, newItem, mapper)
            {
            }

            public new IDocumentKey GetDocumentKey(Document doc, IQueryExecutionContext context)
            {
                return base.GetDocumentKey(doc, context);
            }

            public new T ConvertDocument(Document doc, IQueryExecutionContext context)
            {
                return base.ConvertDocument(doc, context);
            }
        }

        public interface IDocumentMapperWithConverter : IDocumentMapper<Record>, IDocumentKeyConverter
        {
        }
    }
}
