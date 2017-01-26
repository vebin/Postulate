using System;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class TrackChangesAttribute : Attribute
	{
		public TrackChangesAttribute()
		{
		}

		public string IgnoreProperties { get; set; }
	}
}
