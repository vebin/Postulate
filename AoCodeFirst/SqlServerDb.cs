using Postulate.Abstract;
using Postulate.Merge;
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

		public Profiler Profiler { get; set; }

		public override void MergeSchema()
		{
			SchemaMerge sm = new SchemaMerge(this.GetType());
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();				
				sm.Execute(cn);
			}
		}
	}
}
