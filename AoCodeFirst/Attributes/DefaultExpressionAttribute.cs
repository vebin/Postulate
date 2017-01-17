using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DefaultExpressionAttribute : Attribute
	{
		private readonly string _expression;

		public DefaultExpressionAttribute(string expression)
		{
			_expression = expression;
		}

		public string Expression { get { return _expression; } }
	}
}
