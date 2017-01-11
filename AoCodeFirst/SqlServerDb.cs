using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
