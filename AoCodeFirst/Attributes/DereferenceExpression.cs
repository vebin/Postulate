﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	/// <summary>
	/// Indicates what to select from a model class when dereferencing a foreign key value when changes are analyzed by RowManagerBase.GetChanges
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DereferenceExpression : Attribute
	{
		private readonly string _expr;

		public DereferenceExpression(string expression)
		{
			_expr = expression;
		}

		public string Expression { get { return _expr; } }
	}
}
