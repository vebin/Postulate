using Dapper;
using Postulate.Attributes;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;

namespace Postulate.Abstract
{
	public abstract class QueryBase<TResult>
	{
		private readonly string _sql;
		protected readonly SqlDb _db;
		protected DynamicParameters _builtInParams = new DynamicParameters();
		
		public QueryBase(string sql, SqlDb db)
		{
			_sql = sql;
			_db = db;			
		}

		public string Sql { get { return _sql; } }

		public virtual object TestParameters { get { return null; } }

		public virtual Func<IEnumerable<TResult>, bool> TestCondition { get { return (results) => { return true; }; } }

		public IEnumerable<TResult> Execute(IDbConnection connection, object parameters, object criteria = null)
		{
			DynamicParameters dp;
			string query;
			BuildQuery(parameters, criteria, out query, out dp);
			return connection.Query<TResult>(query, dp);
		}

		public TResult ExecuteSingle(IDbConnection connection, object parameters)
		{
			return connection.QuerySingle<TResult>(_sql, parameters);
		}

		protected void BuildQuery(object parameters, object criteria, out string query, out DynamicParameters dp)
		{
			dp = new DynamicParameters(parameters);
			if (_builtInParams.ParameterNames.Any()) dp.AddDynamicParams(_builtInParams);
			query = _sql;

			if (criteria != null)
			{
				dp.AddDynamicParams(criteria);
				string whereClause = GetWhereClause(criteria);
				if (!string.IsNullOrEmpty(whereClause)) query += ((parameters != null) ? " AND " : " WHERE ") + whereClause;
			}
		}

		public bool Test(IDbConnection connection)
		{
			return TestCondition.Invoke(Execute(connection, TestParameters));
		}

		private string GetWhereClause(object criteria)
		{
			var clauses = new List<string>();
			var baseProps = criteria.GetType().BaseType.GetProperties();
			var props = criteria.GetType().GetProperties().Where(pi => !baseProps.Any(bp => bp.Name.Equals(pi.Name)));
			foreach (var prop in props)
			{
				object value = prop.GetValue(criteria);				
				if (value != null)
				{						
					QueryFieldAttribute qf;
					string expr = (prop.HasAttribute(out qf)) ? qf.Expression : $"[{prop.Name}]=@{prop.Name}";
					clauses.Add(expr);					
				}
			}
			
			return string.Join(" AND ", clauses);			
		}
	}
}
