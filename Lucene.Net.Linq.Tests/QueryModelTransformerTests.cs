using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;

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
            Assert.That(transformer.Sort.GetSort()[0].GetField(), Is.EqualTo("Name"));
            Assert.That(transformer.Sort.GetSort()[0].GetType(), Is.EqualTo(SortField.STRING));
            Assert.That(transformer.Sort.GetSort()[0].GetReverse(), Is.False, "Reverse");
        }

        [Test]
        public void ConvertsToSort_Desc()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(1));
            Assert.That(transformer.Sort.GetSort()[0].GetField(), Is.EqualTo("Name"));
            Assert.That(transformer.Sort.GetSort()[0].GetType(), Is.EqualTo(SortField.STRING));
            Assert.That(transformer.Sort.GetSort()[0].GetReverse(), Is.True, "Reverse");
        }

        [Test]
        public void ConvertsToSort_MultipleOrderings()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(string), "Name"), OrderingDirection.Asc));
            orderByClause.Orderings.Add(new Ordering(new LuceneQueryFieldExpression(typeof(int), "Id"), OrderingDirection.Desc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Assert.That(transformer.Sort.GetSort().Length, Is.EqualTo(2));
            Assert.That(transformer.Sort.GetSort()[0].GetField(), Is.EqualTo("Name"));
            Assert.That(transformer.Sort.GetSort()[0].GetType(), Is.EqualTo(SortField.STRING));
            Assert.That(transformer.Sort.GetSort()[0].GetReverse(), Is.False, "Reverse");
            Assert.That(transformer.Sort.GetSort()[1].GetField(), Is.EqualTo("Id"));
            Assert.That(transformer.Sort.GetSort()[1].GetType(), Is.EqualTo(SortField.INT));
            Assert.That(transformer.Sort.GetSort()[1].GetReverse(), Is.True, "Reverse");
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
            Assert.That(transformer.Sort.GetSort()[0].GetField(), Is.EqualTo("Name"));
            Assert.That(transformer.Sort.GetSort()[0].GetType(), Is.EqualTo(SortField.STRING));
            Assert.That(transformer.Sort.GetSort()[0].GetReverse(), Is.False, "Reverse");
            Assert.That(transformer.Sort.GetSort()[1].GetField(), Is.EqualTo("Id"));
            Assert.That(transformer.Sort.GetSort()[1].GetType(), Is.EqualTo(SortField.INT));
            Assert.That(transformer.Sort.GetSort()[1].GetReverse(), Is.True, "Reverse");
        }
    }
}
