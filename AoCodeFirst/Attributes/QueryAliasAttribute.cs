using System;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class QueryAliasAttribute : Attribute
	{
		private readonly string _alias;

		public QueryAliasAttribute(string alias)
		{
			_alias = alias;
		}

		public string Alias { get { return _alias; } }
	}
}
