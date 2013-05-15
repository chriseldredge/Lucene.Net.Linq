using Lucene.Net.Linq.Mapping;

namespace Sample1
{
	/// <remarks>
	/// Note how Article and Comment use a different property for their
	/// key. If they both used Id, Lucene.Net.Linq would have no way
	/// to distinguish them as different types of documents.
	/// </remarks> 
	public class Article
	{
		[Field(Key = true)]
		public long ArticleId { get; set; }
	}

	public class Comment
	{
		[Field(Key = true)]
		public long CommentId { get; set; }
	}
}

namespace Sample2
{
	/// <remarks>
	/// In this example a DocumentKey is added to each class
	/// to add a fixed-value field to each document that will
	/// be used to distinguish entities of different types.
	/// 
	/// In this example both Article and Commend use Id as
	/// the unique identifier for each entity.
	/// </remarks> 
	[DocumentKey(FieldName = "Type", Value = "Article")]
	public class Article
	{
		[Field(Key = true)]
		public long Id { get; set; }
	}

	[DocumentKey(FieldName = "Type", Value = "Comment")]
	public class Comment
	{
		[Field(Key = true)]
		public long Id { get; set; }
	}
}
