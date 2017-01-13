using System;
using System.Configuration;
using System.Data;

namespace Postulate
{
	public abstract class SqlDb
	{
		private readonly string _connectionString;

		public SqlDb(string connectionName)
		{
			_connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
			Initializing();
		}

		protected string ConnectionString
		{
			get { return _connectionString; }
		}

		public abstract IDbConnection GetConnection();

		protected virtual void Initializing()
		{
		}

		protected abstract void MergeSchema(Type dbType);
		protected abstract void MergeSchema(string @namespace);
	}
}
