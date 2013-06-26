using System;
using System.ComponentModel;
using System.Globalization;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected LuceneDataProvider provider;
        protected Directory directory;
        protected IndexWriter writer;
        protected static readonly Version version = Version.LUCENE_29;

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            writer = new IndexWriter(directory, GetAnalyzer(version), IndexWriter.MaxFieldLength.UNLIMITED);
            
            provider = new LuceneDataProvider(directory, writer.Analyzer, version, writer);
        }

        protected virtual Analyzer GetAnalyzer(Version version)
        {
            return new PorterStemAnalyzer(version);
        }

        [DocumentKey(FieldName = "FixedKey", Value = "SampleDocument")]
        public class SampleDocument
        {
            private static int count;
            public SampleDocument()
            {
                Key = (count++).ToString();
            }

            public string Name { get; set; }

            [Field(IndexMode.NotAnalyzed)]
            public string Id { get; set; }

            [Field(Key = true)]
            public string Key { get; set; }

            public int Scalar { get; set; }

            [NumericField]
            public long Long { get; set; }

            public int? NullableScalar { get; set; }
            public bool Flag { get; set; }

            [Field("backing_version", Converter = typeof(VersionConverter))]
            public System.Version Version { get; set; }

            [Field("backing_field")]
            public string Alias { get; set; }

            public SampleGenericOnlyComparable GenericComparable { get; set; }

            public DateTime DateTime { get; set; }

            public DateTime DateTimeOffset { get; set; }
        }

        public class ScoreTrackingSampleDocument : SampleDocument
        {
            [QueryScore]
            public float ScoreProperty { get; set; }
        }

        [TypeConverter(typeof(SampleGenericOnlyComparableConverter))]
        public class SampleGenericOnlyComparable : IComparable<SampleGenericOnlyComparable>
        {
            private readonly int value;

            public SampleGenericOnlyComparable(int value)
            {
                this.value = value;
            }

            public int Value
            {
                get { return value; }
            }

            public int CompareTo(SampleGenericOnlyComparable other)
            {
                return value.CompareTo(other.value);
            }
        }

        public class SampleGenericOnlyComparableConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof (string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return new SampleGenericOnlyComparable(int.Parse(value.ToString()));
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var comp = (SampleGenericOnlyComparable) value;
                return comp != null ? comp.Value.ToString() : null;
            }
        }

        protected void AddDocument<T>(T document) where T : new()
        {
            using (var session = provider.OpenSession<T>())
            {
                session.Add(document);
                session.Commit();
            }
        }
    }
}