using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Sample
{
    public class Article
    {
        [Field(Analyzer = typeof(StandardAnalyzer))]
        public string Author { get; set; }

        [Field(Analyzer = typeof(StandardAnalyzer))]
        public string Title { get; set; }

        public DateTimeOffset PublishDate { get; set; }

        // Stores the field as a NumericField
        [NumericField]
        public long Id { get; set; }

        // Stores the field as text
        public int IssueNumber { get; set; }

        [Field(IndexMode.NotIndexed, Store = StoreMode.Yes)]
        public string BodyText { get; set; }

        // Maps to field "text"
        [Field("text", Store = StoreMode.No, Analyzer = typeof(PorterStemAnalyzer))]
        public string SearchText
        {
            get { return string.Join(" ", new[] {Author, Title, BodyText}); }
        }

        // Stores complex type as string with a given TypeConverter
        [Field(Converter = typeof (VersionConverter))]
        public System.Version Version { get; set; }

        // Add IgnoreFieldAttribute to properties that should not be mapped to/from Document
        [IgnoreField]
        public string IgnoreMe { get; set; }

        [DocumentBoost]
        public float Boost { get; set; }
    }

    [TestFixture]
    public class AttributeConfiguration
    {
        public static void Main()
        {
            var directory = new RAMDirectory();

            var provider = new LuceneDataProvider(directory, Version.LUCENE_30);

            // add some documents
            using (var session = provider.OpenSession<Article>())
            {
                session.Add(new Article {Author = "John Doe", BodyText = "some body text", PublishDate = DateTimeOffset.UtcNow, Boost = 2f});
            }

            var articles = provider.AsQueryable<Article>();

            var threshold = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));

            var articlesByJohn = from a in articles
                                 where a.Author == "John Doe" && a.PublishDate > threshold
                                 orderby a.Title
                                 select a;


            Console.WriteLine("Articles by John Doe: " + articlesByJohn.Count());

            var searchResults = from a in articles
                                where a.SearchText == "some search query"
                                select a;

            Console.WriteLine("Search Results: " + searchResults.Count());
        }

        [Test, Explicit]
        public void RunMain()
        {
            Main();
        }
    }

    public class VersionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof (string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (string.IsNullOrWhiteSpace((string)value)) return null;

            return new System.Version((string)value);
        }
    }
}