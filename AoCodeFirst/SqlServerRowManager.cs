using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using Postulate.Extensions;
using System.Reflection;

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
			DefaultQuery = sg.SelectStatement();

			if (IsMapped())
			{
				InsertCommand = sg.InsertStatement();
				UpdateCommand = sg.UpdateStatement();
				DeleteCommand = sg.DeleteStatement();
			}
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
			return connection.QueryFirstOrDefault<TRecord>($"{DefaultQuery} WHERE {criteria}", parameters);
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

		public void Update(TRecord record, object parameters, params Expression<Func<TRecord, object>>[] setColumns)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				Update(cn, record, parameters, setColumns);
			}
		}

		public override void Update(IDbConnection connection, TRecord record, object parameters, Expression<Func<TRecord, object>>[] setColumns)
		{
			Type modelType = typeof(TRecord);
			IdentityColumnAttribute idAttr;
			string identityCol = (modelType.HasAttribute(out idAttr)) ? idAttr.ColumnName : SqlDb.IdentityColumnName;
			bool useAltIdentity = (!identityCol.Equals(SqlDb.IdentityColumnName));
			PropertyInfo piIdentity = null;
			if (useAltIdentity) piIdentity = modelType.GetProperty(identityCol);

			DynamicParameters dp = new DynamicParameters(parameters);			
			dp.Add(identityCol, (!useAltIdentity) ? record.Id : piIdentity.GetValue(record));

			string setClause = string.Join(", ", setColumns.Select(expr =>
			{
				string propName = PropertyNameFromLambda(expr);
				PropertyInfo pi = typeof(TRecord).GetProperty(propName);
				dp.Add(propName, expr.Compile().Invoke(record));
				return $"[{pi.SqlColumnName()}]=@{propName}";				
			}).Concat(modelType.GetPropertiesWithAttribute<UpdateExpressionAttribute>().Select(pi =>
			{
				UpdateExpressionAttribute attr = pi.GetCustomAttribute<UpdateExpressionAttribute>();
				return $"[{pi.SqlColumnName()}]={attr.Expression}";
			})));
			
			string cmd = $"UPDATE {modelType.DbObjectName(true)} SET {setClause} WHERE [{identityCol}]=@{identityCol}";

			connection.Execute(cmd, dp);
		}

		public IEnumerable<TRecord> Query(string criteria, object parameters, string orderBy, int page = 0)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return Query(cn, criteria, parameters, orderBy, page);
			}
		}

		public override IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, string orderBy, int page = 0)
		{
			return _db.Query<TRecord>(ResolveQuery(criteria, orderBy, page), parameters);
		}

		private string ResolveQuery(string criteria, string orderBy, int page = 0)
		{
			int startRecord = (page * RecordsPerPage) + 1;
			int endRecord = (page * RecordsPerPage) + RecordsPerPage;

			string result = DefaultQuery;
			if (!string.IsNullOrEmpty(criteria)) result += $" WHERE {criteria}";			

			return $"WITH [source] AS ({SqlServerDb.InsertRowNumberColumn(result, orderBy)}) SELECT * FROM [source] WHERE [RowNumber] BETWEEN {startRecord} AND {endRecord};";
		}
	}
}
