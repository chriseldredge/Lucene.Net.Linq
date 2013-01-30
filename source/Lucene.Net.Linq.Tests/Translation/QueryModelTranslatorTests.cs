using System;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Clauses;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Translation;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Rhino.Mocks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Translation
{
    [TestFixture]
    public class QueryModelTranslatorTests
    {
        private IFieldMappingInfoProvider mappingInfo;
        private QueryModelTranslator transformer;
        private readonly QueryModel queryModel = new QueryModel(new MainFromClause("i", typeof(Record), Expression.Constant("r")), new SelectClause(Expression.Constant("a")) );
        private IFieldMappingInfo numericMappingInfo;
        private IFieldMappingInfo nonNumericMappingInfo;

        [SetUp]
        public void SetUp()
        {
            mappingInfo = MockRepository.GenerateStub<IFieldMappingInfoProvider>();

            transformer = new QueryModelTranslator(new Context(new RAMDirectory(), new WhitespaceAnalyzer(), Version.LUCENE_29, null, new object()), mappingInfo);

            numericMappingInfo = MockRepository.GenerateStub<IFieldMappingInfo>();
            numericMappingInfo.Stub(i => i.IsNumericField).Return(true);
            numericMappingInfo.Stub(i => i.SortFieldType).Return(SortField.LONG);
            
            nonNumericMappingInfo = MockRepository.GenerateStub<IFieldMappingInfo>();
            nonNumericMappingInfo.Stub(i => i.IsNumericField).Return(false);
            nonNumericMappingInfo.Stub(i => i.SortFieldType).Return(SortField.STRING);
        }

        [Test]
        public void NoOrderByClauses()
        {
            Assert.That(transformer.Model.Sort, Is.Not.Null);
            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            Assert.That(transformer.Model.Sort.GetSort(), Is.EqualTo(new Sort().GetSort()));
        }

        [Test]
        public void ConvertsToSort()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));
            mappingInfo.Expect(m => m.GetMappingInfo("Name")).Return(nonNumericMappingInfo);
            nonNumericMappingInfo.Stub(i => i.FieldName).Return("Name");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
        }

        [Test]
        public void ConvertsDateTimeOffsetToSort()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(DateTimeOffset?), "Date"), OrderingDirection.Asc));
            mappingInfo.Expect(m => m.GetMappingInfo("Date")).Return(numericMappingInfo);
            numericMappingInfo.Stub(i => i.FieldName).Return("Date");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Date", OrderingDirection.Asc, SortField.LONG);
        }

        [Test]
        public void ConvertsDateTimeToSortNonNumeric()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(DateTimeOffset?), "Date"), OrderingDirection.Asc));
            mappingInfo.Expect(m => m.GetMappingInfo("Date")).Return(nonNumericMappingInfo);
            nonNumericMappingInfo.Stub(i => i.FieldName).Return("Date");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Date", OrderingDirection.Asc, SortField.STRING);
        }

        [Test]
        public void ConvertsToSort_Desc()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Desc));
            mappingInfo.Expect(m => m.GetMappingInfo("Name")).Return(nonNumericMappingInfo);
            nonNumericMappingInfo.Stub(i => i.FieldName).Return("the_name_field");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "the_name_field", OrderingDirection.Desc, SortField.STRING);
        }

        [Test]
        public void ConvertsToSort_CustomSort()
        {
            var mapping = MockRepository.GenerateStub<IFieldMappingInfo>();
            mapping.Stub(i => i.IsNumericField).Return(false);
            mapping.Stub(i => i.SortFieldType).Return(-1);
            mapping.Stub(i => i.PropertyName).Return("Name");
            mapping.Stub(i => i.PropertyType).Return(typeof(string));

            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Desc));
            mappingInfo.Expect(m => m.GetMappingInfo("Name")).Return(mapping);
            mapping.Stub(i => i.FieldName).Return("the_name_field");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "the_name_field", OrderingDirection.Desc, SortField.CUSTOM);
            Assert.That(transformer.Model.Sort.GetSort()[0].ComparatorSource, Is.InstanceOf<NonGenericConvertableFieldComparatorSource>());
        }

        [Test]
        public void ConvertsToSort_MultipleOrderings()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));
            mappingInfo.Expect(m => m.GetMappingInfo("Name")).Return(nonNumericMappingInfo);
            mappingInfo.Expect(m => m.GetMappingInfo("Id")).Return(numericMappingInfo);
            nonNumericMappingInfo.Stub(i => i.FieldName).Return("Name");
            numericMappingInfo.Stub(i => i.FieldName).Return("Id");

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(2));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[1], "Id", OrderingDirection.Desc, SortField.LONG);
        }

        [Test]
        public void ConvertsToSort_MultipleClauses()
        {
            mappingInfo.Expect(m => m.GetMappingInfo("Name")).Return(nonNumericMappingInfo);
            mappingInfo.Expect(m => m.GetMappingInfo("Id")).Return(numericMappingInfo);
            nonNumericMappingInfo.Stub(i => i.FieldName).Return("Name");
            numericMappingInfo.Stub(i => i.FieldName).Return("Id");

            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 1);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(2));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[1], "Id", OrderingDirection.Desc, SortField.LONG);
        }

        [Test]
        public void SetsDocumentTracker()
        {
            var expr = Expression.Constant(this);
            transformer.VisitTrackRetrievedDocumentsClause(new TrackRetrievedDocumentsClause(expr), queryModel, 0);

            Assert.That(transformer.Model.DocumentTracker, Is.SameAs(expr.Value));
        }

        [Test]
        public void SetsQueryFilterOnKeyFields()
        {
            mappingInfo.Expect(m => m.KeyProperties).Return(new[] { "MyProp" });
            mappingInfo.Expect(m => m.GetMappingInfo("MyProp")).Return(new FakeFieldMappingInfo { FieldName = "my-key" });

            transformer.Build(queryModel);
            
            Assert.That(transformer.Model.Filter, Is.Not.Null);
        }

        [Test]
        public void SetsNullQueryFilterOnEmptyKeyFields()
        {
            mappingInfo.Expect(m => m.KeyProperties).Return(new string[0]);

            transformer.Build(queryModel);

            Assert.That(transformer.Model.Filter, Is.Null);
        }

        private void AssertSortFieldEquals(SortField sortField, string expectedFieldName, OrderingDirection expectedDirection, int expectedType)
        {
            Assert.That(sortField.Field, Is.EqualTo(expectedFieldName));
            Assert.That(sortField.Type, Is.EqualTo(expectedType), "SortField type for field " + expectedFieldName);
            Assert.That(sortField.Reverse, Is.EqualTo(expectedDirection == OrderingDirection.Desc), "Reverse");
        }

    }
}
