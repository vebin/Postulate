using System;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class UpdateExpressionAttribute : Attribute
	{
		private string _expression;

		public UpdateExpressionAttribute(string expression)
		{
			_expression = expression;
		}

		public string Expression { get { return _expression; } }
	}
}
