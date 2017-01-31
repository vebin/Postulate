using Dapper;
using Postulate.Abstract;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Postulate
{
	public class SqlServerQuery<TResult> : QueryBase<TResult>
	{
		public SqlServerQuery(string sql, SqlServerDb db) : base (sql, db)
		{
		}

		public IEnumerable<TResult> Execute(object parameters, object criteria = null)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return Execute(cn, parameters, criteria);
			}
		}

		public TResult ExecuteSingle(object parameters)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return ExecuteSingle(cn, parameters);
			}
		}

		public IEnumerable<TResult> Execute(object parameters, string orderBy, int pageSize, int page = 0, object criteria = null)
		{
			DynamicParameters dp;
			string query;
			BuildQuery(parameters, criteria, out query, out dp);
			return ((SqlServerDb)_db).Query<TResult>(query, dp, orderBy, pageSize, page);
		}
	}
}
