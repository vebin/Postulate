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
	}
}
