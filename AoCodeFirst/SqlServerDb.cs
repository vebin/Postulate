using Dapper;
using Postulate.Abstract;
using Postulate.Merge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using static Dapper.SqlMapper;

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

		#region Query methods
		public IEnumerable<T> Query<T>(string query, object parameters)
		{
			CommandDefinition cmdDef = new CommandDefinition(query, parameters);
			IEnumerable<T> results = null;
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				Profiler?.Start(cn, cmdDef);
				results = cn.Query<T>(query, parameters);
				Profiler?.Stop(cn);
			}
			return results;
		}

		public IEnumerable<dynamic> Query(string query, object parameters)
		{
			IEnumerable<dynamic> results = null;
			CommandDefinition cmdDef = new CommandDefinition(query, parameters);
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				Profiler?.Start(cn, cmdDef);
				results = cn.Query(query, parameters);
				Profiler?.Stop(cn);
				cn.Close();
			}
			return results;
		}

		public IEnumerable<T> Query<T>(string baseQuery, object parameters, string orderBy, int pageSize, int page = 0)
		{
			IEnumerable<T> results = null;
			int startRecord = (page * pageSize) + 1;
			int endRecord = (page * pageSize) + pageSize;

			string query = $"WITH [source] AS ({InsertRowNumberColumn(baseQuery, orderBy)}) SELECT * FROM [source] WHERE [RowNumber] BETWEEN {startRecord} AND {endRecord};";

			CommandDefinition cmdDef = new CommandDefinition(query, parameters);
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				Profiler?.Start(cn, cmdDef);
				results = cn.Query<T>(query, parameters);
				Profiler?.Stop(cn);
				cn.Close();
			}
			return results;
		}

		public void QueryMultiple(string queries, object parameters, Action<GridReader> action, CommandType commandType = CommandType.StoredProcedure)
		{
			CommandDefinition cmdDef = new CommandDefinition(queries, parameters);

			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				Profiler?.Start(cn, cmdDef);
				GridReader grid = cn.QueryMultiple(new CommandDefinition(queries, parameters, commandType: commandType));
				Profiler?.Stop(cn);
				action.Invoke(grid);
			}
		}

		public void QueryMultiple<T>(IEnumerable<QueryBase<T>> queries, object parameters, Action<GridReader> action, CommandType commandType = CommandType.StoredProcedure)
		{
			var queriesJoined = string.Join("\r\n", queries.Select(q => q.Sql));
			QueryMultiple(queriesJoined, parameters, action, commandType);
		}
		#endregion

		#region execute methods
		public int RunProcedure(string procedure, object parameters)
		{
			return ExecuteInner(procedure, parameters, CommandType.StoredProcedure);
		}

		public int RunProcedure(string procedure, DynamicParameters parameters)
		{
			return ExecuteInner(procedure, parameters, CommandType.StoredProcedure);
		}

		public int RunStatement(string statement, object parameters)
		{
			return ExecuteInner(statement, parameters, CommandType.Text);
		}

		private int ExecuteInner(string procedure, object parameters, CommandType commandType)
		{
			int result = 0;
			CommandDefinition cmdDef = new CommandDefinition(procedure, parameters, commandType: commandType);
			using (SqlConnection cn = GetConnection() as SqlConnection)
			{
				cn.Open();
				Profiler?.Start(cn, cmdDef);
				result = cn.Execute(new CommandDefinition(procedure, parameters, commandType: commandType));
				Profiler?.Stop(cn);
				cn.Close();
			}
			return result;
		}
		#endregion

		public static string InsertRowNumberColumn(string query, string orderBy)
		{
			StringBuilder sb = new StringBuilder(query);
			int insertPoint = query.ToLower().IndexOf("select ") + "select ".Length;
			sb.Insert(insertPoint, $"ROW_NUMBER() OVER(ORDER BY {orderBy}) AS [RowNumber], ");
			return sb.ToString();
		}
	}
}
