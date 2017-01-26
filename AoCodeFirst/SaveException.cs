using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	public class SaveException : Exception
	{
		private readonly string _command;
		private readonly object _parameters;

		public SaveException(string message, string command, object parameters, Exception innerException) : base(message, innerException)
		{
			_command = command;
			_parameters = parameters;
		}

		public string Command { get { return _command; } }

		public string ParamInfo
		{
			get
			{
				string result = null;
				DynamicParameters dp = _parameters as DynamicParameters;
				if (dp != null)
				{
					result = string.Join(", ", dp.ParameterNames.Select(p => $"{p} = {dp.Get<object>(p)}"));
				}
				else
				{
					Type t = _parameters.GetType();
					result = string.Join(", ", t.GetProperties().Select(pi => $"{pi.Name} = {pi.GetValue(_parameters)}"));
				}

				return result;
			}
		}
	}
}
