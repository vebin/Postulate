using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Abstract
{
	public abstract class QueryBase<T>
	{
		private readonly string _sql;
		protected readonly SqlDb _db;
		
		public QueryBase(string sql, SqlDb db)
		{
			_sql = sql;
			_db = db;
		}

		public string Sql { get { return _sql; } }

		public virtual object TestParameters { get { return null; } }

		public virtual Func<IEnumerable<T>, bool> TestCondition { get { return (r) => r.Any(); } }

		public IEnumerable<T> Execute(IDbConnection connection, object parameters)
		{
			return connection.Query<T>(_sql, parameters);
		}

		public bool Test(IDbConnection connection)
		{
			return TestCondition.Invoke(Execute(connection, TestParameters));
		}
	}
}
