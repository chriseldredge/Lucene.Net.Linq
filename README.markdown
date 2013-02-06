LINQ to Lucene.Net
=====

Lucene.Net.Linq is a .net library that enables LINQ queries to run natively on a Lucene.Net index.

* Automatically converts PONOs to Documents and back
* Add, delete and update documents in atomic transaction
* Unit of Work pattern automatically tracks and flushes updated documents
* Update/replace documents with \[Field(Key=true)\] to prevent duplicates
* Term queries
* Prefix queries
* Range queries and numeric range queries
* Complex boolean queries
* Native pagination using Skip and Take
* Support storing and querying NumericField 
* Automatically convert complex types for storing, querying and sorting
* Custom boost functions using IQueryable<T>.Boost() extension method
* Sort by standard string, NumericField or any type that implements IComparable
* Sort by item.Score() extension method to sort by relevance
* Specify custom format for DateTime stored as strings
* Register cache-warming queries to be executed when IndexSearcher is being reloaded

Available on NuGet Gallery
--------------------------

To install the [Lucene.Net.Linq package](http://nuget.org/packages/Lucene.Net.Linq),
run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

    PM> Install-Package Lucene.Net.Linq

Example
----------

First, create a plain old .net object that maps to your index. Use attributes to customize
the field name and each field is stored and indexed.

    using System;
    using Lucene.Net.Linq.Mapping;

    public class Article
    {
        public string Author { get; set; }

        [Field(Analyzer = typeof(StandardAnalyzer))]
        public string Title { get; set; }
        public DateTimeOffset PublishDate { get; set; }

        // Stores the field as a NumericField and use it
        // as document key.
        [NumericField(Key = true)]
        public long Id { get; set; }

        // Stores the field as text
        public int IssueNumber { get; set; }

        [Field(IndexMode.NotIndexed, Store = true)]
        public string BodyText { get; set; }

        // Maps to field "text"
        [Field("text", Store = false, Analyzer = typeof(PorterStemAnalyzer))]
        public string SearchText
        {
            get { return string.Join(" ", new[] { Author, Title, BodyText }); }
        }

        // Store complex type as string with a given TypeConverter
        [Field(Converter = typeof(VersionConverter))]
        public Version Version { get; set; }

        // Add IgnoreFieldAttribute to properties that should not be mapped to/from Document
        [IgnoreField]
        public string IgnoreMe { get; set; }
    }

Next, create LuceneDataProvider and run some queries:

    using System.Linq;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.Linq;
    using Lucene.Net.Store;
    using Version = Lucene.Net.Util.Version;

    public class SampleProgram
    {
        public static void Main()
        {
            var directory = new RAMDirectory();

            var provider = new LuceneDataProvider(directory, Version.LUCENE_30);

            // add some documents
            using (var session = provider.OpenSession<Article>())
            {
                session.Add(new Article { Author = "John Doe", BodyText = "some body text", PublishDate = DateTimeOffset.UtcNow });
            }

            var articles = provider.AsQueryable<Article>();

            var threshold = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));

            var articlesByJohn = from a in articles
                          where a.Author == "John Doe" && a.PublishDate > threshold
                          orderby a.Title
                          select a;
 
            var searchResults = from a in articles
                                 where a.SearchText == "some search query"
                                 select a;

            provider.Dispose();
        }
    }

Use a PerFieldAnalyzerWrapper to control how queries and fields are analyzed. For the above example, fields like SearchText (text) should
generally be analyzed using a stemming analyzer, but fields like Id, IssueNumber and Version should be indexed using a keyword analyzer.

Storing multiple document types
-------------------------------

Different document types can be stored in a single index as long as each type uses different properties/fields for their keys.

Example:

    public class Article
    {
        [Field(Key = true)]
        public long ArticleId { get; set; }


    }

    public class Comment
    {
        [Field(Key = true)]
        public long CommentId { get; set; }

        /* snip */
    }

Alternatively, a \[DocumentKey\] attribute may be applied to the class to give
it a unique field/value from other documents:

    [DocumentKey(FieldName = "Type", Value = "Article")]
    public class Article
    {
        [Field(Key = true)]
        public long Id { get; set; }

        /* snip */
    }

    [DocumentKey(FieldName = "Type", Value = "Comment")]
    public class Comment
    {
        [Field(Key = true)]
        public long Id { get; set; }

        /* snip */
    }

Upcoming features / ideas
-------------------------

* Ability to capture query statistics like execution time, total hits, top score, etc.
* Ability to use faceting to group results by field or natively return distinct field values

Known issues 
------------

LINQ is staggeringly complex and this provider supports a tiny subset of possible expression trees.

Not supported:

* Subqueries, grouping, etc.
* Overly complex predicates
* etc.

The following dynamic boost query is not supported:

    session.Query().Select(i => new { i.Id, i.Author, i.Citations }).Boost(i => i.Citations)

