using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Clauses;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Translation;
using Lucene.Net.Search;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests.Translation
{
    [TestFixture]
    public class QueryModelTranslatorTests
    {
        private IFieldMappingInfoProvider mappingProvider;
        private QueryModelTranslator transformer;
        private readonly QueryModel queryModel = new QueryModel(new MainFromClause("i", typeof(Record), Expression.Constant("r")), new SelectClause(Expression.Constant("a")) );

        [SetUp]
        public void SetUp()
        {
            mappingProvider = MockRepository.GenerateStub<IFieldMappingInfoProvider>();

            transformer = new QueryModelTranslator(mappingProvider);
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
            ExpectSortOnProperty("Name", SortField.STRING, OrderingDirection.Asc);

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
        }

        [Test]
        public void ConvertsDateTimeOffsetToSort()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(DateTimeOffset?), "Date"), OrderingDirection.Asc));
            ExpectSortOnProperty("Date", SortField.LONG, OrderingDirection.Asc);

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Date", OrderingDirection.Asc, SortField.LONG);
        }

        [Test]
        public void ConvertsToSort_Desc()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Desc));
            ExpectSortOnProperty("Name", SortField.STRING, OrderingDirection.Desc);

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Desc, SortField.STRING);
        }

        [Test]
        public void ConvertsToSort_MultipleOrderings()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));

            ExpectSortOnProperty("Name", SortField.STRING, OrderingDirection.Asc);
            ExpectSortOnProperty("Id", SortField.LONG, OrderingDirection.Desc);

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Model.Sort.GetSort().Length, Is.EqualTo(2));
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
            AssertSortFieldEquals(transformer.Model.Sort.GetSort()[1], "Id", OrderingDirection.Desc, SortField.LONG);
        }

        [Test]
        public void ConvertsToSort_MultipleClauses()
        {

            ExpectSortOnProperty("Name", SortField.STRING, OrderingDirection.Asc);
            ExpectSortOnProperty("Id", SortField.LONG, OrderingDirection.Desc);

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
        public void SetsQueryFilterOnKeyField()
        {
            mappingProvider.Expect(m => m.KeyProperties).Return(new[] { "MyProp" });
            mappingProvider.Expect(m => m.GetMappingInfo("MyProp")).Return(
                new FakeFieldMappingInfo { FieldName = "my-key" });

            transformer.Build(queryModel);

            var filter = (QueryWrapperFilter)transformer.Model.Filter;

            Assert.That(filter, Is.Not.Null, "transformer.Model.Filter");
            Assert.That(filter.ToString(), Is.EqualTo("QueryWrapperFilter(+my-key:*)"));
        }

        [Test]
        public void SetsQueryFilterOnKeyFieldWithConstraint()
        {
            mappingProvider.Expect(m => m.KeyProperties).Return(new[] { "MyProp" });
            mappingProvider.Expect(m => m.GetMappingInfo("MyProp")).Return(
                new DocumentKeyFieldMapper<string>("my-key", "fixed-value"));

            transformer.Build(queryModel);

            var filter = (QueryWrapperFilter)transformer.Model.Filter;

            Assert.That(filter, Is.Not.Null, "transformer.Model.Filter");
            Assert.That(filter.ToString(), Is.EqualTo("QueryWrapperFilter(+my-key:fixed-value)"));
        }

        [Test]
        public void SetsNullQueryFilterOnEmptyKeyFields()
        {
            mappingProvider.Expect(m => m.KeyProperties).Return(new string[0]);

            transformer.Build(queryModel);

            Assert.That(transformer.Model.Filter, Is.Null);
        }

        private void AssertSortFieldEquals(SortField sortField, string expectedFieldName, OrderingDirection expectedDirection, int expectedType)
        {
            Assert.That(sortField.Field, Is.EqualTo(expectedFieldName));
            Assert.That(sortField.Type, Is.EqualTo(expectedType), "SortField type for field " + expectedFieldName);
            Assert.That(sortField.Reverse, Is.EqualTo(expectedDirection == OrderingDirection.Desc), "Reverse");
        }

        private void ExpectSortOnProperty(string propertyName, int sortType, OrderingDirection direction)
        {
            var mappingInfo = MockRepository.GenerateStub<IFieldMappingInfo>();
            mappingProvider.Expect(m => m.GetMappingInfo(propertyName)).Return(mappingInfo);
            mappingInfo.Stub(i => i.FieldName).Return(propertyName);
            mappingInfo.Stub(i => i.CreateSortField(direction == OrderingDirection.Desc)).Return(new SortField(propertyName, sortType, direction == OrderingDirection.Desc));
        }
    }
}
