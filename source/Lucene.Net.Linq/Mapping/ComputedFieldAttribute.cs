using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Mapping
{
	/// <summary>
	/// Maps a property to a <see cref="IComputedField"/> so that the field
	/// can be calulated into a queryable field without existing on the index.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ComputedFieldAttribute : Attribute
	{
		/// <param name="fieldComputer">a
		/// A custom <see cref="IComputedField"/> implementation that can convert the property
		/// to a Lucene.Net query and from a Lucene.Net document.
		/// </param>
		public ComputedFieldAttribute(Type fieldComputer)
		{
			FieldComputerInstance = (IComputedField)Activator.CreateInstance(fieldComputer);
		}

		internal IComputedField FieldComputerInstance { get; set; }
	}
}
