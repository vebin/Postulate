using Postulate.Abstract;
using Postulate.Merge;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Postulate
{
	public class SqlServerDb : SqlDb
	{
		public SqlServerDb(string connectionName) : base(connectionName)
		{
		}

		public override IDbConnection GetConnection()
		{
			return new SqlConnection(ConnectionString);
		}

		protected override void MergeSchema(Type dbType)
		{
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				SchemaMerge sm = new SchemaMerge(dbType, cn);
				sm.Execute(cn);
			}
		}
	}
}
