using System.Linq.Expressions;
using Lucene.Net.Linq.Transformation;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Rhino.Mocks;

namespace Lucene.Net.Linq.Tests.Transformation
{
    [TestFixture]
    public class QueryModelTransformerTests
    {
        private static readonly ConstantExpression constantExpression = Expression.Constant(true);
        private static readonly WhereClause whereClause = new WhereClause(constantExpression);
        private ExpressionVisitor visitor1;
        private ExpressionVisitor visitor2;
        private QueryModelTransformer transformer;
        private readonly QueryModel queryModel = new QueryModel(new MainFromClause("i", typeof(Record), Expression.Constant("r")), new SelectClause(Expression.Constant("a")) );
        private MockRepository mocks;

        [SetUp]
        public void SetUp()
        {
            mocks = new MockRepository();

            visitor1 = mocks.StrictMock<ExpressionVisitor>();
            visitor2 = mocks.StrictMock<ExpressionVisitor>();
            var visitors = new[] { visitor1, visitor2 };
            transformer = new QueryModelTransformer(visitors, visitors);

            using (mocks.Ordered())
            {
                visitor1.Expect(v => v.Visit(whereClause.Predicate)).Return(whereClause.Predicate);
                visitor2.Expect(v => v.Visit(whereClause.Predicate)).Return(whereClause.Predicate);
            }

            mocks.ReplayAll();
        }

        [Test]
        public void VisitsWhereClause()
        {
            transformer.VisitWhereClause(whereClause, queryModel, 0);

            Verify();
        }

        [Test]
        public void VisitsOrderByClause()
        {
            var orderByClause = new OrderByClause();
            orderByClause.Orderings.Add(new Ordering(constantExpression, OrderingDirection.Asc));

            transformer.VisitOrderByClause(orderByClause, queryModel, 0);

            Verify();
        }

        private void Verify()
        {
            mocks.VerifyAll();
        }
    }
}
