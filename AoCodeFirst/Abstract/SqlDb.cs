using System;
using System.Configuration;
using System.Data;

namespace Postulate.Abstract
{
	public abstract class SqlDb
	{
		private readonly string _connectionString;

		public const string IdentityColumnName = "Id";

		public SqlDb(string connectionName)
		{
			_connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;			
		}

		protected string ConnectionString
		{
			get { return _connectionString; }
		}		

		public abstract IDbConnection GetConnection();

		public abstract void MergeSchema();
	}
}
