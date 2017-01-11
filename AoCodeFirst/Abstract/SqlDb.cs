using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	public abstract class SqlDb
	{
		private readonly string _connectionString;

		public SqlDb(string connectionName)
		{
			_connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
		}

		protected string ConnectionString
		{
			get { return _connectionString; }
		}

		public abstract IDbConnection GetConnection();
	}
}
