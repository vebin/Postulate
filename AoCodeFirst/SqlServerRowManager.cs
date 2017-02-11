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
using Postulate.Merge;
using Postulate.Models;

namespace Postulate
{
	public class SqlServerRowManager<TRecord, TKey> : RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		private const string _changesSchema = "changes";

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

		protected override TRecord OnFind(IDbConnection connection, TKey id)
		{
			return connection.QueryFirstOrDefault<TRecord>(FindCommand, new { id = id });
		}

		public bool ExistsWhere(string criteria, object parameters, string userName = null)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return ExistsWhere(cn, criteria, parameters, userName);
			}
		}

		public TRecord FindWhere(string criteria, object parameters)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return FindWhere(cn, criteria, parameters);
			}
		}

		protected override TRecord OnFindWhere(IDbConnection connection, string criteria, object parameters)
		{
			return connection.QueryFirstOrDefault<TRecord>($"{DefaultQuery} WHERE {criteria}", parameters);
		}

		public void Save(TRecord record, out SaveAction action, object parameters = null, string userName = null)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				Save(cn, record, out action, parameters, userName);
			}
		}

		public void Save(TRecord record, object parameters = null, string userName = null)
		{
			SaveAction action;
			Save(record, out action, parameters, userName);
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
			DynamicParameters dp = new DynamicParameters(record);
			try
			{				
				if (parameters != null) dp.AddDynamicParams(parameters);
				return connection.QueryFirst<TKey>(InsertCommand, dp);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, InsertCommand, dp, exc);
			}
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
			DynamicParameters dp = new DynamicParameters(record);
			try
			{
				if (parameters != null) dp.AddDynamicParams(parameters);
				connection.Execute(UpdateCommand, dp);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, UpdateCommand, dp, exc);
			}
		}

		public void Delete(TRecord record, object parameters = null, string userName = null)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				Delete(cn, record, parameters, userName);
			}
		}

		protected override void OnDelete(IDbConnection connection, TRecord record, object parameters = null)
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

		protected override void OnUpdate(IDbConnection connection, TRecord record, object parameters, Expression<Func<TRecord, object>>[] setColumns)
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

		public void CaptureChanges(TRecord record)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				CaptureChanges(cn, record);
			}
		}

		protected override void OnCaptureChanges(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes, string userName = null)
		{
			SqlConnection cn = connection as SqlConnection;
			
			if (!cn.Exists("[sys].[schemas] WHERE [name]=@name", new { name = _changesSchema })) cn.Execute($"CREATE SCHEMA [{_changesSchema}]");

			var typeMap = new Dictionary<Type, string>()
			{
				{ typeof(int), "int" },
				{ typeof(long), "bigint" },
				{ typeof(Guid), "uniqueidentifier" }
			};

			DbObject obj = DbObject.FromType(typeof(TRecord));
			string tableName = $"{obj.Schema}_{obj.Name}";

			if (!cn.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = $"{tableName}_Versions" }))
			{
				cn.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}_Versions] (
					[RecordId] {typeMap[typeof(TKey)]} NOT NULL,
					[NextVersion] int NOT NULL DEFAULT (1),
					CONSTRAINT [PK_{_changesSchema}_{tableName}_Versions] PRIMARY KEY ([RecordId])
				)");
			}

			if (!cn.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = tableName }))
			{
				cn.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}] (
					[RecordId] {typeMap[typeof(TKey)]} NOT NULL,
					[Version] int NOT NULL,
					[UserName] nvarchar(256) NOT NULL,
					[ColumnName] nvarchar(100) NOT NULL,
					[OldValue] nvarchar(max) NULL,
					[NewValue] nvarchar(max) NULL,
					[DateTime] datetime NOT NULL DEFAULT (getutcdate()),
					CONSTRAINT [PK_{_changesSchema}_{obj.Name}] PRIMARY KEY ([RecordId], [Version], [ColumnName])
				)");
			}

			int version = 0;
			while (version == 0)
			{
				version = cn.QueryFirstOrDefault<int>($"SELECT [NextVersion] FROM [{_changesSchema}].[{tableName}_Versions] WHERE [RecordId]=@id", new { id = id });
				if (version == 0) cn.Execute($"INSERT INTO [{_changesSchema}].[{tableName}_Versions] ([RecordId]) VALUES (@id)", new { id = id });				
			}
			cn.Execute($"UPDATE [{_changesSchema}].[{tableName}_Versions] SET [NextVersion]=[NextVersion]+1 WHERE [RecordId]=@id", new { id = id });

			foreach (var change in changes)
			{
				cn.Execute(
					$@"INSERT INTO [{_changesSchema}].[{tableName}] ([RecordId], [Version], [UserName], [ColumnName], [OldValue], [NewValue])
					VALUES (@id, @version, @userName, @columnName, @oldValue, @newValue)",
					new
					{
						id = id, version = version,
						columnName = change.PropertyName,
						userName = userName ?? "<unknown>",
						oldValue = CleanMinDate(change.OldValue) ?? "<null>",
						newValue = CleanMinDate(change.NewValue) ?? "<null>"
					});
			}
		}

		private static object CleanMinDate(object value)
		{
			// prevents DateTime.MinValue from getting passed to SQL Server as a parameter, where it fails
			if (value is DateTime && value.Equals(default(DateTime))) return null;
			return value;
		}

		protected override object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
		{
			object result = base.OnGetChangesPropertyValue(propertyInfo, record, connection);

			ForeignKeyAttribute fk;
			DereferenceExpression dr;
			if (result != null && propertyInfo.HasAttribute(out fk) && fk.PrimaryTableType.HasAttribute(out dr))
			{
				DbObject obj = DbObject.FromType(fk.PrimaryTableType);
				result = connection.QueryFirst<string>(
					$@"SELECT {dr.Expression} FROM [{obj.Schema}].[{obj.Name}] 
					WHERE [{fk.PrimaryTableType.IdentityColumnName()}]=@id", new { id = result });
			}

			return result;
		}

		public IEnumerable<ChangeHistory<TKey>> QueryChangeHistory(TKey id, int timeZoneOffset = 0)
		{
			using (SqlConnection cn = _db.GetConnection() as SqlConnection)
			{
				cn.Open();
				return QueryChangeHistory(cn, id, timeZoneOffset);
			}
		}

		public override IEnumerable<ChangeHistory<TKey>> QueryChangeHistory(IDbConnection connection, TKey id, int timeZoneOffset = 0)
		{
			DbObject obj = DbObject.FromType(typeof(TRecord));
			string tableName = $"{obj.Schema}_{obj.Name}";

			var results = connection.Query<ChangeHistoryRecord<TKey>>(
				$@"SELECT * FROM [{_changesSchema}].[{tableName}] WHERE [RecordId]=@id ORDER BY [DateTime] DESC", new { id = id });

			return results.GroupBy(item => new 
			{
				RecordId = item.RecordId,				
				Version = item.Version				
			}).Select(ch =>
			{
				return new ChangeHistory<TKey>()
				{
					RecordId = ch.Key.RecordId,
					DateTime = ch.First().DateTime.AddHours(timeZoneOffset),
					Version = ch.Key.Version,
					Properties = ch.Select(chr => new PropertyChange()
					{
						PropertyName = chr.ColumnName,
						OldValue = chr.OldValue,
						NewValue = chr.NewValue
					})
				};
			});
		}
	}
}
