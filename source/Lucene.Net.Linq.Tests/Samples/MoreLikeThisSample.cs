using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using Lucene.Net.Search.Similar;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Sample
{
    [TestFixture]
    [Explicit]
    public class MoreLikeThisSample
    {
        public class Entity
        {
            [Field(Analyzer=typeof(StandardAnalyzer), TermVector = TermVectorMode.Yes)]
            public string Text { get; set; }
        }

        [Test]
        public void Demo()
        {
            var provider = new LuceneDataProvider(new RAMDirectory(), Version.LUCENE_30);

            using (var session = provider.OpenSession<Entity>())
            {
                session.Add(new Entity { Text = "text is comprised of words"});
                session.Add(new Entity { Text = "words compose text"});
                session.Add(new Entity { Text = "completely unrelated"});
            }

            var mapper = new MoreLikeThisDocumentMapper<Entity>(Version.LUCENE_30);

            var query = provider.AsQueryable(mapper);

            var result = query.First(e => e.Text == "words");

            var moreLikeQuery = mapper.MoreLike(result);
            var moreLikeResults = query.Where(moreLikeQuery).Select(e => e.Text).ToList();

            Assert.That(moreLikeResults, Is.EquivalentTo(new[] { "text is comprised of words", "words compose text" }));
        }

        class MoreLikeThisDocumentMapper<T> : ReflectionDocumentMapper<T>
        {
            private readonly IDictionary<T, Query> queries = new Dictionary<T, Query>();
            private MoreLikeThis mlt;

            public MoreLikeThisDocumentMapper(Version version) : base(version)
            {
            }

            public override void PrepareSearchSettings(IQueryExecutionContext context)
            {
                mlt = new MoreLikeThis(context.Searcher.IndexReader);
                mlt.MinDocFreq = 2;
                mlt.MinTermFreq = 1;
                mlt.Analyzer = new StandardAnalyzer(Version.LUCENE_30);
                mlt.SetFieldNames(new[] {"Text"});
                base.PrepareSearchSettings(context);
            }

            public override void ToObject(Document source, IQueryExecutionContext context, T target)
            {
                base.ToObject(source, context, target);
                Console.WriteLine(context.Searcher.DocFreq(new Term("Text", "words")));
                queries[target] = mlt.Like(context.CurrentScoreDoc.Doc);
            }

            public Query MoreLike(T item)
            {
                return queries[item];
            }
        }
    }
}