using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Dapper;
using Postulate.Abstract;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Enums;

namespace Postulate
{
	public class SqlServerRowManager<TRecord, TKey> : RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		private readonly string _connectionString;		

		public SqlServerRowManager(string connectionString)
		{
			_connectionString = connectionString;
			SqlServerGenerator<TRecord, TKey> sg = new SqlServerGenerator<TRecord, TKey>();
			FindCommand = sg.FindStatement();
			InsertCommand = sg.InsertStatement();
			UpdateCommand = sg.UpdateStatement();
			DeleteCommand = sg.DeleteStatement();

			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				if (!TableExists(cn)) cn.Execute(sg.CreateTableStatement(false));
			}
		}

		public TRecord Find(TKey id)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return Find(cn, id);
			}
		}

		public override TRecord Find(IDbConnection connection, TKey id)
		{
			return connection.QueryFirstOrDefault<TRecord>(FindCommand, new { id = id });
		}

		public TRecord FindWhere(string criteria, object parameters)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return FindWhere(cn, criteria, parameters);
			}
		}

		public override TRecord FindWhere(IDbConnection connection, string criteria, object parameters)
		{
			return connection.QueryFirst<TRecord>($"{DefaultQuery} WHERE {criteria}", parameters);
		}

		public void Save(TRecord record, out SaveAction action, object parameters = null)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				Save(cn, record, out action, parameters);
			}
		}

		public void Save(TRecord record, object parameters = null)
		{
			SaveAction action;
			Save(record, out action, parameters);
		}

		public TKey Insert(TRecord record)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return Insert(cn, record);
			}
		}

		protected override TKey OnInsert(IDbConnection connection, TRecord record, object parameters = null)
		{
			DynamicParameters dp = new DynamicParameters(record); ;
			if (parameters != null) dp.AddDynamicParams(parameters);
			return connection.QueryFirst<TKey>(InsertCommand, dp);
		}

		public void Update(TRecord record)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				Update(cn, record);
			}
		}

		protected override void OnUpdate(IDbConnection connection, TRecord record, object parameters = null)
		{
			DynamicParameters dp = new DynamicParameters(record); ;
			if (parameters != null) dp.AddDynamicParams(parameters);
			connection.Execute(UpdateCommand, dp);
		}

		public void Delete(TRecord record, object parameters = null)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				Delete(cn, record, parameters);
			}
		}

		public override void Delete(IDbConnection connection, TRecord record, object parameters = null)
		{
			DynamicParameters dp = new DynamicParameters(parameters);
			dp.Add("ID", record.ID);
			connection.Execute(DeleteCommand, dp);
		}

		public override void Update(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns, object parameters = null)
		{
			SqlServerGenerator<TRecord, TKey> sg = new SqlServerGenerator<TRecord, TKey>();
			//connection.Execute(sg.UpdateStatement(), record);
		}

		public IEnumerable<TRecord> Query(string criteria, object parameters, int page = 0)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return Query(cn, criteria, parameters, page);
			}
		}

		public override IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, int page = 0)
		{
			throw new NotImplementedException();
		}

		public bool TableExists()
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return TableExists(cn);
			}
		}

		public override bool TableExists(IDbConnection connection)
		{
			Type t = typeof(TRecord);
			TableAttribute attr = t.GetCustomAttribute<TableAttribute>();			
			string tableName = (attr != null) ? attr.Name : t.Name;
			string schema = (attr != null && !string.IsNullOrEmpty(attr.Schema)) ? attr.Schema : "dbo";
			return (connection.QueryFirstOrDefault<int?>(
				"SELECT 1 FROM [sys].[tables] WHERE [name]=@name AND SCHEMA_NAME([schema_id])=@schema",
				new { name = tableName, schema = schema }) ?? 0).Equals(1);
		}
	}
}
