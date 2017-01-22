using Postulate.Abstract;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Postulate
{
	public class SqlServerQuery<T> : QueryBase<T>
	{
		public SqlServerQuery(string sql, SqlServerDb db) : base (sql, db)
		{
		}

		public IEnumerable<T> Execute(object parameters)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return Execute(cn, parameters);
			}
		}
	}
}
