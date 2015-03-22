using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Linq.Converters;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Samples
{
	public class ComputedFieldSample
	{
		public class User
		{
			[Field("Username", Key = true, Store = StoreMode.Yes)]
			public string Username { get; set; }

			[ComputedField(typeof(StatusField))]
			public string Status { get; set; }

			[NumericField("ActiveFrom", Converter = typeof(DateTimeToTicksConverter))]
			public DateTime ActiveFrom { get; set; }

			[NumericField("ActiveUntil", Converter = typeof(DateTimeToTicksConverter))]
			public DateTime ActiveUntil { get; set; }
		}

		public class StatusField : IComputedField
		{
			public object GetFieldValue(Document document)
			{
				var activeFrom = new DateTime(Convert.ToInt64(document.GetField("ActiveFrom").StringValue));
				var activeUntil = new DateTime(Convert.ToInt64(document.GetField("ActiveUntil").StringValue));

				return activeFrom <= DateTime.UtcNow && activeUntil >= DateTime.UtcNow
					? "Active"
					: "Inactive";
			}

			public Query CreateQuery(string pattern)
			{
				var query = new BooleanQuery();

				if (pattern == "Active")
				{
					query.Add(NumericRangeQuery.NewLongRange("ActiveFrom", DateTime.MinValue.Ticks, DateTime.UtcNow.Ticks, true, true), Occur.MUST);
					query.Add(NumericRangeQuery.NewLongRange("ActiveUntil", DateTime.UtcNow.Ticks, DateTime.MaxValue.Ticks, true, true), Occur.MUST);
				}
				else if (pattern == "Inactive")
				{
					query.Add(NumericRangeQuery.NewLongRange("ActiveFrom", DateTime.UtcNow.Ticks, DateTime.MaxValue.Ticks, false, true), Occur.SHOULD);
					query.Add(NumericRangeQuery.NewLongRange("ActiveUntil", DateTime.MinValue.Ticks, DateTime.UtcNow.Ticks, true, false), Occur.SHOULD);
				}

				return query;
			}

			public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
			{
				throw new NotImplementedException();
			}

			public SortField CreateSortField(bool reverse)
			{
				throw new NotImplementedException();
			}

			public string ConvertToQueryExpression(object value)
			{
				return (string)value;
			}
		}

		[TestFixture]
		public class ComputedFieldSampleTests
		{
			private Directory directory;
			private LuceneDataProvider provider;

			[SetUp]
			public void Setup()
			{
				directory = new RAMDirectory();
				provider = new LuceneDataProvider(directory, Version.LUCENE_30);

				using (var session = provider.OpenSession<User>())
				{
					session.Add(
						new User { Username = "ActiveUser1", ActiveFrom = DateTime.MinValue, ActiveUntil = DateTime.MaxValue },
						new User { Username = "ActiveUser2", ActiveFrom = DateTime.UtcNow.AddDays(-1), ActiveUntil = DateTime.MaxValue },
						new User { Username = "ActiveUser3", ActiveFrom = DateTime.MinValue, ActiveUntil = DateTime.UtcNow.AddDays(1) },
						new User { Username = "InactiveUser1", ActiveFrom = DateTime.UtcNow.AddDays(1), ActiveUntil = DateTime.MaxValue },
						new User { Username = "InactiveUser2", ActiveFrom = DateTime.MinValue, ActiveUntil = DateTime.UtcNow.AddDays(-1) }
					);
				}
			}

			[TearDown]
			public void Teardown()
			{
				if (provider != null)
				{
					provider.Dispose();
				}

				if (directory != null)
				{
					directory.Dispose();
				}
			}

			[Test]
			public void OnlyActiveRecordsShouldBeReturned()
			{
				var results = provider
					.AsQueryable<User>()
					.Where(x => x.Status == "Active")
					.ToList();

				Assert.That(results.Count() != 0);
				Assert.That(results.All(x => x.Username.StartsWith("Active")));
			}

			[Test]
			public void OnlyInactiveRecordsShouldBeReturned()
			{
				var results = provider
					.AsQueryable<User>()
					.Where(x => x.Status == "Inactive")
					.ToList();

				Assert.That(results.Count() != 0);
				Assert.That(results.All(x => x.Username.StartsWith("Inactive")));
			}
		}
	}
}
