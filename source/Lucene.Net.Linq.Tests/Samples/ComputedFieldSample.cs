using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Linq.Converters;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;
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
				return new SortField(String.Empty, new StatusComparatorSource("ActiveFrom", "ActiveUntil"), reverse);
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

			[Test]
			public void NoRecordsShouldBeReturned()
			{
				var results = provider
					.AsQueryable<User>()
					.Where(x => x.Status == "NotAStatus")
					.ToList();

				Assert.That(!results.Any());
			}

			[Test]
			public void OrderByStatusAscending()
			{
				var results = provider
					.AsQueryable<User>()
					.OrderBy(x => x.Status)
					.ToList();

				var changes = 0;
				for (var i = 1; i < results.Count; i++)
				{
					if (results[i].Status != results[i - 1].Status)
					{
						changes++;
					}
				}

				Assert.IsTrue(changes == 1);
				Assert.IsTrue(results.First().Status == "Active");
				Assert.IsTrue(results.Last().Status == "Inactive");
			}

			[Test]
			public void OrderByStatusDescending()
			{
				var results = provider
					.AsQueryable<User>()
					.OrderByDescending(x => x.Status)
					.ToList();

				var changes = 0;
				for (var i = 1; i < results.Count; i++)
				{
					if (results[i].Status != results[i - 1].Status)
					{
						changes++;
					}
				}

				Assert.IsTrue(changes == 1);
				Assert.IsTrue(results.First().Status == "Inactive");
				Assert.IsTrue(results.Last().Status == "Active");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public class StatusComparatorSource : FieldComparatorSource
		{
			private readonly string activeFromField;
			private readonly string activeUntilField;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="activeFromField"></param>
			/// <param name="activeUntilField"></param>
			public StatusComparatorSource(string activeFromField, string activeUntilField)
			{
				this.activeFromField = activeFromField;
				this.activeUntilField = activeUntilField;
			}

			/// <inheritDoc />
			public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
			{
				return new StatusComparator(numHits, this.activeFromField, this.activeUntilField);
			}
		}

		public class StatusComparator : FieldComparator
		{
			internal struct Status
			{
				public Int64 ActiveFrom { get; set; }
				public Int64 ActiveUntil { get; set; }
			}

			private readonly String activeFromField;
			private readonly String activeUntilField;
			private readonly Status[] values;
			private Status[] currentReaderValues;
			private Status bottom;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="numHits"></param>
			/// <param name="activeFromField"></param>
			/// <param name="activeUntilField"></param>
			public StatusComparator(Int32 numHits, String activeFromField, String activeUntilField)
			{
				this.activeFromField = activeFromField;
				this.activeUntilField = activeUntilField;
				this.values = new Status[numHits];
				this.bottom = new Status();
			}

			/// <inheritDoc />
			public override Int32 Compare(Int32 slot1, Int32 slot2)
			{
				return Compare(
					this.values[slot1],
					this.values[slot2]
				);
			}

			private static Int32 Compare(Status record1, Status record2)
			{
				var now = DateTime.UtcNow.Ticks;

				var record1Active = IsActive(now, record1);
				var record2Active = IsActive(now, record2);

				if (!record1Active && record2Active)
				{
					return 1;
				}

				if (record1Active && !record2Active)
				{
					return -1;
				}

				return 0;
			}

			private static Boolean IsActive(Int64 now, Status record)
			{
				return record.ActiveFrom <= now && record.ActiveUntil >= now;
			}

			/// <inheritDoc />
			public override void SetBottom(Int32 slot)
			{
				this.bottom = this.values[slot];
			}

			/// <inheritDoc />
			public override Int32 CompareBottom(Int32 doc)
			{
				return Compare(
					this.bottom,
					this.currentReaderValues[doc]
				);
			}

			/// <inheritDoc />
			public override void Copy(Int32 slot, Int32 doc)
			{
				this.values[slot] = this.currentReaderValues[doc];
			}

			/// <inheritDoc />
			public override void SetNextReader(IndexReader reader, Int32 docBase)
			{
				var activeFroms = FieldCache_Fields.DEFAULT.GetLongs(reader, this.activeFromField);
				var activeUntils = FieldCache_Fields.DEFAULT.GetLongs(reader, this.activeUntilField);

				this.currentReaderValues = new Status[activeFroms.Length];

				for (var i = 0; i < activeFroms.Length; i++)
				{
					this.currentReaderValues[i].ActiveFrom = activeFroms[i];
					this.currentReaderValues[i].ActiveUntil = activeUntils[i];
				}
			}

			/// <inheritDoc />
			public override IComparable this[Int32 slot]
			{
				get
				{
					return IsActive(DateTime.UtcNow.Ticks, this.values[slot]);
				}
			}
		}
	}
}
