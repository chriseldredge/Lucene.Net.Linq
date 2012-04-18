using System;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Search;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class QueryModelTranslatorTests
    {
        private QueryModelTranslator transformer;
        private readonly QueryModel queryModel = new QueryModel(new MainFromClause("i", typeof(Record), Expression.Constant("r")), new SelectClause(Expression.Constant("a")) );

        [SetUp]
        public void SetUp()
        {
            transformer = new QueryModelTranslator(new Context(new WhitespaceAnalyzer(), Version.LUCENE_29));
        }

        [Test]
        public void NoOrderByClauses()
        {
            Assert.That(transformer.Sort, Is.Not.Null);
            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(1));
            Assert.That(transformer.Sort.GetSort(), Is.EqualTo(new Sort().GetSort()));
        }

        [Test]
        public void ConvertsToSort()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
        }

        [Test]
        public void ConvertsDateTimeOffsetToSort()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(DateTimeOffset?), "Date"), OrderingDirection.Asc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Sort.GetSort()[0], "Date", OrderingDirection.Asc, SortField.LONG);
        }

        [Test]
        public void ConvertsToSort_Desc()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(1));
            AssertSortFieldEquals(transformer.Sort.GetSort()[0], "Name", OrderingDirection.Desc, SortField.STRING);
        }

        [Test]
        public void ConvertsToSort_MultipleOrderings()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(2));
            AssertSortFieldEquals(transformer.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
            AssertSortFieldEquals(transformer.Sort.GetSort()[1], "Id", OrderingDirection.Desc, SortField.INT);
        }

        [Test]
        public void ConvertsToSort_MultipleClauses()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 1);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(2));
            AssertSortFieldEquals(transformer.Sort.GetSort()[0], "Name", OrderingDirection.Asc, SortField.STRING);
            AssertSortFieldEquals(transformer.Sort.GetSort()[1], "Id", OrderingDirection.Desc, SortField.INT);
        }

        private void AssertSortFieldEquals(SortField sortField, string expectedFieldName, OrderingDirection expectedDirection, int expectedType)
        {
            Assert.That(sortField.GetField(), Is.EqualTo(expectedFieldName));
            Assert.That(sortField.GetType(), Is.EqualTo(expectedType));
            Assert.That(sortField.GetReverse(), Is.EqualTo(expectedDirection == OrderingDirection.Desc), "Reverse");
        }

    }
}
