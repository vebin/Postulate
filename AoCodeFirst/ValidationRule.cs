using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	/// <summary>
	/// Defines a rule that an instance of T must pass when saving
	/// </summary>
	/// <typeparam name="T">Model class to which rule applies</typeparam>
	public class ValidationRule<T>
	{		
		private Func<IDbConnection, T, bool> _rule;
		private string _errorMessage;
	
		public ValidationRule(Func<IDbConnection, T, bool> rule, string errorMessage)
		{
			_rule = rule;
			_errorMessage = errorMessage;
		}

		public string ErrorMessage
		{
			get { return _errorMessage; }
		}

		public bool Validate(IDbConnection connection, T row)
		{	
			return _rule.Invoke(connection, row);
		}
	}
}
