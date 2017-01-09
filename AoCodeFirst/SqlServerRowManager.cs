using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Dapper;
using Postulate.Abstract;

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
			return connection.QueryFirst<TRecord>(FindCommand, new { id = id });
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

		public TKey Insert(TRecord record)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return Insert(cn, record);
			}
		}

		protected override TKey InsertExecute(IDbConnection connection, TRecord record)
		{
			return connection.QueryFirst<TKey>(InsertCommand, record);
		}

		public void Update(TRecord record)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				Update(cn, record);
			}
		}

		protected override void UpdateExecute(IDbConnection connection, TRecord record)
		{
			connection.Execute(UpdateCommand, record);
		}

		public void Delete(TRecord record)
		{
			using (SqlConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				Delete(cn, record);
			}
		}

		public override void Delete(IDbConnection connection, TRecord record)
		{
			connection.Execute(DeleteCommand, new { ID = record.ID });
		}

		public override void Update(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns)
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
	}
}
