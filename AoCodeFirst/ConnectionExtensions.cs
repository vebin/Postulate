using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Postulate
{
	public static class ConnectionExtensions
	{
		public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters) ?? 0) == 1);
		}
	}
}
