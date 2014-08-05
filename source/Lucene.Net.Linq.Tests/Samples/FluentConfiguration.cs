using System;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Tests.Integration;
using Version = Lucene.Net.Util.Version;

namespace Sample
{
    public class FluentConfiguration
    {
        public void CreateMapping()
        {
            var map = new ClassMap<Package>(Version.LUCENE_30);

            map.Key(p => p.Id);
            map.Key(p => p.Version).ConvertWith(new VersionConverter());

            map.Property(p => p.Description)
                .AnalyzeWith(new PorterStemAnalyzer(Version.LUCENE_30))
                .WithTermVector.PositionsAndOffsets();

            map.Property(p => p.DownloadCount)
                .AsNumericField()
                .WithPrecisionStep(8);

            map.Property(p => p.IconUrl).NotIndexed();

            map.Score(p => p.Score);

            map.DocumentBoost(p => p.Boost);
        }

        public class Package
        {
            public string Id { get; set; }

            public Version Version { get; set; }

            public Uri IconUrl { get; set; }

            public string Description { get; set; }

            public int DownloadCount { get; set; }

            public float Score { get; set; }

            public float Boost { get; set; }
        }
    }
}