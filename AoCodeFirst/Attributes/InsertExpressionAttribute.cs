﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class InsertExpressionAttribute : Attribute
	{
		private readonly string _expression;
		private bool _hasParams;

		public InsertExpressionAttribute(string expression, bool hasParameters = false)
		{
			_expression = expression;
			_hasParams = expression.Contains("@");
		}

		public string Expression { get { return _expression; } }

		public bool HasParameters { get { return _hasParams; } }
	}
}
