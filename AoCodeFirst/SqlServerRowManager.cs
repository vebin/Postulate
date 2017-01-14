﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Dapper;
using Postulate.Abstract;
using Postulate.Enums;
using Postulate.Extensions;
using Postulate.Merge;

namespace Postulate
{
	public class SqlServerRowManager<TRecord, TKey> : RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		protected readonly SqlServerDb _db;

		public SqlServerRowManager(SqlServerDb db)
		{
			_db = db;
			SqlServerGenerator<TRecord, TKey> sg = new SqlServerGenerator<TRecord, TKey>();
			FindCommand = sg.FindStatement();
			InsertCommand = sg.InsertStatement();
			UpdateCommand = sg.UpdateStatement();
			DeleteCommand = sg.DeleteStatement();
		}

		public TRecord Find(TKey id)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				Delete(cn, record, parameters);
			}
		}

		public override void Delete(IDbConnection connection, TRecord record, object parameters = null)
		{
			DynamicParameters dp = new DynamicParameters(parameters);
			dp.Add("ID", record.Id);
			connection.Execute(DeleteCommand, dp);
		}

		public override void Update(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns, object parameters = null)
		{
			SqlServerGenerator<TRecord, TKey> sg = new SqlServerGenerator<TRecord, TKey>();
			//connection.Execute(sg.UpdateStatement(), record);
		}

		public IEnumerable<TRecord> Query(string criteria, object parameters, int page = 0)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
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
