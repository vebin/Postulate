using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	/// <summary>
	/// Use this to add a DEFAULT clause within a CREATE or ALTER TABLE statement
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ColumnDefaultAttribute : Attribute
	{
		private readonly string _constExpr;

		public ColumnDefaultAttribute(string constantExpression)
		{
			_constExpr = constantExpression;
		}

		public string Expression { get { return _constExpr; } }
	}
}
