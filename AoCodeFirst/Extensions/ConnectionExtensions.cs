using System.Data;
using Dapper;

namespace Postulate.Extensions
{
	public static class ConnectionExtensions
	{
		public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters) ?? 0) == 1);
		}
	}
}
