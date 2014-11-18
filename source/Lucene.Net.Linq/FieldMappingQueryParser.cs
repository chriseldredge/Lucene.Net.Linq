using System;
using System.Collections.Generic;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    public class FieldMappingQueryParser<T> : QueryParser
    {
        private readonly Version matchVersion;
        private readonly IDocumentMapper<T> mapper;

        public FieldMappingQueryParser(Version matchVersion, IDocumentMapper<T> mapper)
            : base(matchVersion, typeof(FieldMappingQueryParser<T>).FullName + ".DEFAULT_FIELD", mapper.Analyzer)
        {
            this.matchVersion = matchVersion;
            this.mapper = mapper;
        }

        /// <summary>
        /// Sets the default property for queries that don't specify which field to search.
        /// For an example query like <c>Lucene OR NuGet</c>, if this property is set to <c>SearchText</c>,
        /// it will produce a query like <c>SearchText:Lucene OR SearchText:NuGet</c>.
        /// </summary>
        public string DefaultSearchProperty { get; set; }

        public Version MatchVersion
        {
            get { return matchVersion; }
        }

        public IDocumentMapper<T> DocumentMapper
        {
            get { return mapper; }
        }

        public override string Field
        {
            get { return DefaultSearchProperty; }
        }

        protected override Query GetFieldQuery(string field, string queryText)
        {
            var mapping = GetMapping(field);

            try
            {
                var codedQueryText = mapping.ConvertToQueryExpression(queryText);
                return mapping.CreateQuery(codedQueryText);
            }
            catch (Exception ex)
            {
                throw new ParseException(ex.Message, ex);
            }
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            var rangeType = inclusive ? RangeType.Inclusive : RangeType.Exclusive;
            var mapping = GetMapping(field);
            try
            {
                return mapping.CreateRangeQuery(part1, part2, rangeType, rangeType);
            }
            catch (Exception ex)
            {
                throw new ParseException(ex.Message, ex);
            }
        }

        protected virtual IFieldMappingInfo GetMapping(string field)
        {
            if (field == typeof (FieldMappingQueryParser<T>).FullName + ".DEFAULT_FIELD")
            {
                field = DefaultSearchProperty;
            }

            try
            {
                return mapper.GetMappingInfo(field);
            }
            catch (KeyNotFoundException)
            {
                throw new ParseException("Unrecognized field: '" + field + "'");
            }
        }
    }
}
