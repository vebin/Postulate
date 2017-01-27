using Postulate.Abstract;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper;
using System.ComponentModel.DataAnnotations;
using static Postulate.Merge.AddColumns;
using Postulate.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postulate.Merge
{
	public enum MergeObjectType
	{
		Table,
		Column,
		PrimaryKey,
		ForeignKey
	}

	public enum MergeActionType
	{
		Create,
		Rename,
		Retype,
		Delete
	}

	internal delegate IEnumerable<SchemaMerge.Action> GetSchemaMergeActionHandler(IEnumerable<Type> modelTypes, IDbConnection connection);

	public class SchemaMerge
	{
		private readonly IEnumerable<Type> _modelTypes;		

		private List<DbObject> _createdTables;
		private List<DbObject> _deletedTables;

		public IEnumerable<Action> AllActions { get; private set; }
		public ILookup<Action, string> AllValidationErrors { get; private set; }
		public ILookup<Action, string> AllCommands { get; private set; }

		public SchemaMerge(Type dbType, string @namespace = null)
		{			
			_createdTables = new List<DbObject>();
			_deletedTables = new List<DbObject>();

			_modelTypes = dbType.Assembly.GetTypes()
				.Where(t =>
					!t.Name.StartsWith("<>") &&
					t.Namespace.Equals(@namespace ?? dbType.Namespace) &&
					!t.HasAttribute<NoSchemaMergeAttribute>() &&
					!t.IsAbstract &&					
					t.IsDerivedFromGeneric(typeof(DataRecord<>)));			
		}

		public IEnumerable<Type> ModelTypes { get { return _modelTypes; } }

		public void SaveAs(IDbConnection connection, string fileName)
		{
			using (StreamWriter writer = File.CreateText(fileName))
			{
				writer.Write(GetSqlScript(connection));
			}
		}

		public IEnumerable<Action> Analyze(IDbConnection connection)
		{
			var actions = new List<Action>();
			_createdTables = new List<DbObject>();

			GetSchemaMergeActionHandler[] methods = new GetSchemaMergeActionHandler[]
			{
				GetDeletedTables, GetNewTables, GetNewColumns, GetRetypedColumns,
				GetDeletedColumns, GetRekeyedTables/*
				GetRenamedTables, GetRenamedColumns, 
				GetNewPrimaryKeys, GetDeletedPrimaryKeys*/
			};

			foreach (var m in methods) actions.AddRange(m.Invoke(_modelTypes, connection));

			AllActions = actions;

			AllValidationErrors = actions.SelectMany(action => action.ValidationErrors()
				.Select(err => new { Action = action, Message = err }))
				.ToLookup(item => item.Action, item => item.Message);

			AllCommands = actions.SelectMany(action => action.SqlCommands()
				.Select(cmd => new { Action = action, Command = cmd }))
				.ToLookup(item => item.Action, item => item.Command);

			return actions;
		}

		public void Execute(IDbConnection connection)
		{
			var actions = Analyze(connection);

			if (actions.Any(a => !a.IsValid()))
			{
				string message = string.Join("\r\n", ValidationErrors(actions));					
				throw new ValidationException($"The model has one or more validation errors:\r\n{message}");
			}

			foreach (var a in actions)
			{
				Console.WriteLine(a.ToString());
				foreach (var cmd in a.SqlCommands())
				{					
					Console.WriteLine($"+ {cmd}");
					Console.WriteLine();
					connection.Execute(cmd);
				}
			}

			CreateForeignKeys(connection);
		}

		private IEnumerable<string> GetForeignKeys(IDbConnection connection)
		{
			foreach (var t in _modelTypes)
			{
				foreach (var pi in CreateForeignKey.GetForeignKeys(t))
				{
					string fkName = pi.ForeignKeyName();
					if (!connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fkName }))
					{
						var fk = new CreateForeignKey(pi);
						foreach (var cmd in fk.SqlCommands()) yield return cmd;
					}
				}
			}
		}

		private void CreateForeignKeys(IDbConnection connection)
		{
			foreach (var cmd in GetForeignKeys(connection)) connection.Execute(cmd);
		}

		public IEnumerable<ValidationError> ValidationErrors(IEnumerable<Action> actions)
		{
			return actions.Where(a => !a.IsValid()).SelectMany(a => a.ValidationErrors(), (a, m) => new ValidationError(a, m));
		}

		private IEnumerable<Action> GetNewTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> actions = new List<Action>();

			foreach (var type in modelTypes)
			{
				DbObject obj = DbObject.FromType(type);
				if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = obj.Schema, name = obj.Name }))
				{
					_createdTables.Add(obj);
					actions.Add(new CreateTable(type));
				}
			}

			return actions;
		}

		private IEnumerable<Action> GetDeletedPrimaryKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetNewPrimaryKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetDeletedColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var schemaColumns = GetSchemaColumns(connection);

			var modelColumns = GetModelColumns(modelTypes);

			var deletedColumns = schemaColumns.Where(sc => 
				!modelColumns.Any(mc => mc.Equals(sc)) && // model column does not exist
				!_deletedTables.Contains(new DbObject(sc.Schema, sc.TableName)) && // table has been deleted
				modelColumns.Any(mc => mc.Schema.Equals(sc.Schema) && mc.TableName.Equals(sc.TableName)) // but the containing table still does
				);

			results.AddRange(deletedColumns.Select(col => new DropColumn(col, connection)));

			return results;
		}

		private IEnumerable<ColumnRef> GetModelColumns(IEnumerable<Type> types)
		{
			return types.SelectMany(t => t.GetProperties().Select(pi => new ColumnRef(pi)));
		}

		private IEnumerable<Action> GetDeletedTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var allTables = connection.Query(
				@"SELECT SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [TableName], [object_id] AS [ObjectID] 
				FROM [sys].[tables] 
				WHERE 
					[name] NOT LIKE 'AspNet%' 
					AND [name] NOT LIKE '__MigrationHistory' AND 
					SCHEMA_NAME([schema_id])<>'changes'")
				.Select(tbl => new DbObject(tbl.Schema, tbl.TableName) { ObjectID = tbl.ObjectID });

			var deletedTables = allTables.Where(obj => !modelTypes.Any(mt => obj.Equals(mt)));

			_deletedTables.AddRange(deletedTables);
			results.AddRange(deletedTables.Select(del => new DropTable(del, connection)));

			return results;
		}

		private IEnumerable<Action> GetRetypedColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var schemaColumns = GetSchemaColumns(connection);
			var modelColumns = GetModelColumns(modelTypes);

			var retypedColumns = from sc in schemaColumns
								 join mc in modelColumns on sc equals mc
								 where 
									!sc.DataTypeSyntax().ToLower().Equals(mc.PropertyInfo.SqlColumnType().ToLower()) &&
									!_createdTables.Contains(new DbObject(mc.Schema, mc.TableName))
								 select new { ModelColumn = mc, SchemaColumn = sc };

			results.AddRange(retypedColumns.Select(col => new RetypeColumn(col.SchemaColumn, col.ModelColumn)));

			return results;
		}

		private IEnumerable<Action> GetRenamedColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetRenamedTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetRekeyedTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var schemaPKcols = GetSchemaPrimaryKeys(connection);
			var modelPKcols = GetModelPrimaryKeys(modelTypes, connection);

			var newPKs = modelPKcols.Where(mpk => !schemaPKcols.Any(spk => mpk.Equals(spk)));
			foreach (var pk in newPKs)
			{
				results.Add(new DropTable(new DbObject(pk.Schema, pk.TableName) { ObjectID = pk.ObjectId }, connection));
				results.Add(new CreateTable(pk.ModelType));
			}

			return results;
		}

		private IEnumerable<Action> GetNewColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var schemaColumns = GetSchemaColumns(connection);

			var dbObjects = schemaColumns.GroupBy(item => new DbObject(item.Schema, item.TableName) { ObjectID = item.ObjectID });
			var dcObjectIDs = dbObjects.ToDictionary(obj => obj.Key, obj => obj.Key.ObjectID);

			Dictionary<DbObject, Type> dcModelTypes = new Dictionary<DbObject, Type>();

			var modelColumns = modelTypes.SelectMany(mt => mt.GetProperties()
				.Where(pi => CreateTable.IsSupportedType(pi.PropertyType))
				.Select(pi =>
				{
					DbObject obj = DbObject.FromType(mt);
					if (!dcModelTypes.ContainsKey(obj)) dcModelTypes.Add(obj, mt);
					return new ColumnRef()
					{
						Schema = obj.Schema,
						TableName = obj.Name,
						ColumnName = pi.SqlColumnName(),
						PropertyInfo = pi
					};
				}));

			var newColumns = modelColumns.Where(mcol =>
				!_createdTables.Contains(new DbObject(mcol.Schema, mcol.TableName)) &&				
				!schemaColumns.Any(scol => mcol.Equals(scol)));

			foreach (var colGroup in newColumns.GroupBy(item => new DbObject(item.Schema, item.TableName)))
			{
				if (IsTableEmpty(connection, colGroup.Key.Schema, colGroup.Key.Name) || dcModelTypes[colGroup.Key].HasAttribute<AllowDropAttribute>())
				{
					_deletedTables.Add(colGroup.Key);
					_createdTables.Add(colGroup.Key);
					colGroup.Key.ObjectID = dcObjectIDs[colGroup.Key];
					results.Add(new DropTable(colGroup.Key, connection));
					results.Add(new CreateTable(dcModelTypes[colGroup.Key]));
				}
				else
				{
					results.Add(new AddColumns(dcModelTypes[colGroup.Key], colGroup, connection));
				}
			}

			return results;
		}

		private static IEnumerable<PrimaryKeyRef> GetSchemaPrimaryKeys(IDbConnection connection)
		{
			var pkColumns = connection.Query(
				@"SELECT 
					SCHEMA_NAME([tbl].[schema_id]) AS [Schema], [tbl].[name] AS [TableName], [col].[name] AS [ColumnName], [tbl].[object_id] AS [ObjectId]
				FROM 
					[sys].[indexes] [pk] INNER JOIN [sys].[index_columns] [pk_col] ON 
						[pk].[object_id]=[pk_col].[object_id] AND
						[pk].[index_id]=[pk_col].[index_id]
					INNER JOIN [sys].[objects] [tbl] ON [pk].[object_id]=[tbl].[object_id]
					INNER JOIN [sys].[columns] [col] ON 
						[pk_col].[object_id]=[col].[object_id] AND
						[pk_col].[column_id]=[col].[column_id]
				WHERE
					[is_primary_key]=1", null);

			return pkColumns
				.GroupBy(pkcol => new { Schema = pkcol.Schema, TableName = pkcol.TableName })				
				.Select(grp => new PrimaryKeyRef()
				{
					Schema = grp.Key.Schema,
					TableName = grp.Key.TableName,
					ObjectId = grp.First().ObjectId,
					ColumnList = string.Join(",", grp.Select(item => item.ColumnName))
				});
		}

		private static IEnumerable<PrimaryKeyRef> GetModelPrimaryKeys(IEnumerable<Type> types, IDbConnection connection)
		{
			return types.Select(t =>
			{
				DbObject obj = DbObject.FromType(t, connection);
				return new PrimaryKeyRef()
				{
					Schema = obj.Schema,
					TableName = obj.Name,
					ColumnList = string.Join(",", CreateTable.PrimaryKeyColumns(t)),
					ObjectId = obj.ObjectID,
					ModelType = t
				};
			});
		}

		private static IEnumerable<ColumnRef> GetSchemaColumns(IDbConnection connection)
		{
			return connection.Query<ColumnRef>(
				@"SELECT SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [TableName], [c].[Name] AS [ColumnName], 
					[t].[object_id] AS [ObjectID], TYPE_NAME([c].[system_type_id]) AS [DataType], 
					[c].[max_length] AS [ByteLength], [c].[is_nullable] AS [IsNullable],
					[c].[precision] AS [Precision], [c].[scale] as [Scale]
				FROM 
					[sys].[tables] [t] INNER JOIN [sys].[columns] [c] ON [t].[object_id]=[c].[object_id]", null);
		}

		private bool IsTableEmpty(IDbConnection connection, string schema, string tableName)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{schema}].[{tableName}]", null) ?? 0) == 0);
		}

		public string GetSqlScript(IDbConnection connection)
		{
			var actions = Analyze(connection);
			StringBuilder sb = new StringBuilder();
			foreach (var action in actions)
			{
				foreach (var cmd in action.SqlCommands())
				{
					sb.Append(cmd);
					sb.AppendLine("\r\nGO");
					sb.AppendLine();
				}
			}

			foreach (var cmd in GetForeignKeys(connection))
			{
				sb.Append(cmd);
				sb.AppendLine("\r\nGO");
				sb.AppendLine();
			}

			return sb.ToString();
		}

		public abstract class Action
		{
			private readonly MergeObjectType _objectType;
			private readonly MergeActionType _actionType;
			private readonly string _name;

			public Action(MergeObjectType objectType, MergeActionType actionType, string name)
			{
				_objectType = objectType;
				_actionType = actionType;
				_name = name;
			}

			public MergeObjectType ObjectType { get { return _objectType; } }
			public MergeActionType ActionType { get { return _actionType; } }

			public abstract IEnumerable<string> ValidationErrors();

			public bool IsValid()
			{
				return !ValidationErrors().Any();
			}

			public override string ToString()
			{
				return $"{_actionType} {_objectType}: {_name}";
			}

			public abstract IEnumerable<string> SqlCommands();
		}

		public class ValidationError
		{
			private readonly Action _action;
			private readonly string _message;

			public ValidationError(Action action, string message)
			{
				_action = action;
				_message = message;
			}

			public Action Action { get { return _action; } }

			public override string ToString()
			{
				return $"{_action.ToString()}: {_message}";
			}
		}
	}
}
