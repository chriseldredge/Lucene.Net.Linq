namespace Sample
{
    using System;
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
            var writer = new IndexWriter(directory, new StandardAnalyzer(Version.LUCENE_29), IndexWriter.MaxFieldLength.UNLIMITED);

            var provider = new LuceneDataProvider(directory, writer.GetAnalyzer(), Version.LUCENE_29, writer);

            // add some documents
            using (var session = provider.OpenSession<Article>())
            {
                session.Add(new Article { Author = "John Doe", BodyText = "some body text", PublishDate = DateTimeOffset.UtcNow });
            }

            var articles = provider.AsQueryable<Article>();

            var threshold = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));

            var articlesByJohn = from a in articles
                          where a.Author == "John Smith" && a.PublishDate > threshold
                          orderby a.Title
                          select a;

            var searchResults = from a in articles
                                 where a.SearchText == "some search query"
                                 select a;

        }
    }
}

namespace Sample
{
    using System;
    using Lucene.Net.Linq.Mapping;

    public class Article
    {
        public string Author { get; set; }
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
        [Field("text", Store = StoreMode.No)]
        public string SearchText
        {
            get { return string.Join(" ", new[] { Author, Title, BodyText }); }
        }

        // Stores complex type as string with a given TypeConverter
        [Field(Converter = typeof(VersionConverter))]
        public Version Version { get; set; }

        // Add IgnoreFieldAttribute to properties that should not be mapped to/from Document
        [IgnoreField]
        public string IgnoreMe { get; set; }
    }
}

namespace Sample
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    public class VersionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return new Version((string)value);
        }
    }
}