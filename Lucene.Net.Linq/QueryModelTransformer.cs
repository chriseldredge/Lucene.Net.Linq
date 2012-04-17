using System.Collections.Generic;
using Lucene.Net.Linq.Transformers;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Transforms various expressions in a QueryModel instance to make it easier to convert into a Lucene Query.
    /// </summary>
    public class QueryModelTransformer : QueryModelVisitorBase
    {
        private readonly IEnumerable<ExpressionTreeVisitor> visitors;

        private QueryModelTransformer()
            : this(new ExpressionTreeVisitor[]
                       {
                           new QuerySourceReferenceTransformingTreeVisitor(),
                           new NoOpMethodCallRemovingTreeVisitor(),
                           new MethodCallToBinaryExpressionTreeVisitor(),
                           new NullSafetyConditionRemovingTreeVisitor()
                       })
        {
        }

        private QueryModelTransformer(IEnumerable<ExpressionTreeVisitor> visitors)
        {
            this.visitors = visitors;
        }

        public static void TransformQueryModel(QueryModel queryModel)
        {
            var instance = new QueryModelTransformer();

            var copy = queryModel.Clone();

            queryModel.Accept(instance);

#if DEBUG
            System.Diagnostics.Trace.WriteLine("Pre-transformed QueryModel: " + copy, "Lucene.Net.Linq");
            System.Diagnostics.Trace.WriteLine("Transformed QueryModel:     " + queryModel, "Lucene.Net.Linq");
#endif
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            foreach (var v in visitors)
                whereClause.TransformExpressions(v.VisitExpression);
            
            base.VisitWhereClause(whereClause, queryModel, index);
        }
    }
}