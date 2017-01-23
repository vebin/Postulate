using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	/// <summary>
	/// Lets you specify an expression to use with QueryBase when a property name alone won't work (for example when you need to refer to a query alias)
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class QueryFieldAttribute : Attribute
	{
		private readonly string _expr;

		public QueryFieldAttribute(string expression)
		{
			_expr = expression;
		}

		public string Expression { get { return _expr; } }
	}
}
