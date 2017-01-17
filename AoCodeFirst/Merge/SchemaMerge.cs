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
		private readonly List<Action> _actions;

		public SchemaMerge(Type dbType, IDbConnection connection)
		{
			IDbConnection cn = connection;
			_modelTypes = dbType.Assembly.GetTypes()
				.Where(t =>
					!t.Name.StartsWith("<>") &&
					t.Namespace.Equals(dbType.Namespace) &&					
					!t.IsAbstract &&					
					t.IsDerivedFromGeneric(typeof(DataRecord<>)));

			GetSchemaMergeActionHandler[] methods = new GetSchemaMergeActionHandler[]
			{
				GetDeletedTables, GetNewTables, GetNewColumns/*
				GetRenamedTables, GetRenamedColumns, GetRetypedColumns, GetDeletedColumns,
				GetNewPrimaryKeys, GetDeletedForeignKeys, GetDeletedPrimaryKeys*/
			};

			_actions = new List<Action>();
			foreach (var m in methods) _actions.AddRange(m.Invoke(_modelTypes, cn));
		}

		public IEnumerable<Action> Actions { get { return _actions; } }

		public void SaveAs(string fileName)
		{
			using (StreamWriter writer = File.CreateText(fileName))
			{
				writer.Write(ToString());
			}
		}

		public void Execute(IDbConnection connection)
		{
			if (_actions.Any(a => !a.IsValid()))
			{
				string message = string.Join("\r\n", ValidationErrors());					
				throw new ValidationException($"The model has one or more validation errors:\r\n{message}");
			}

			foreach (var a in _actions)
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

		private void CreateForeignKeys(IDbConnection connection)
		{
			foreach (var t in _modelTypes)
			{
				foreach (var pi in CreateForeignKey.GetForeignKeys(t))
				{
					string fkName = pi.ForeignKeyName();
					if (!connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fkName }))
					{
						var fk = new CreateForeignKey(pi);
						foreach (var cmd in fk.SqlCommands()) connection.Execute(cmd);
					}
				}
			}
		}

		public IEnumerable<ValidationError> ValidationErrors()
		{
			return _actions.Where(a => !a.IsValid()).SelectMany(a => a.ValidationErrors(), (a, m) => new ValidationError(a, m));
		}

		private IEnumerable<Action> GetNewTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> actions = new List<Action>();

			foreach (var type in modelTypes)
			{
				DbObject obj = DbObject.FromType(type);
				if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = obj.Schema, name = obj.Name }))
				{					
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
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetDeletedTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var allTables = connection.Query(
				"SELECT SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [TableName], [object_id] AS [ObjectID] FROM [sys].[tables]")
				.Select(tbl => new DbObject(tbl.Schema, tbl.TableName) { ObjectID = tbl.ObjectID });

			var deletedTables = allTables.Where(obj => !modelTypes.Any(mt => obj.Equals(mt)));			
			_deletedTables.AddRange(deletedTables);
										
			results.AddRange(deletedTables.Select(del => new DropTable(del, connection)));

			return results;
		}

		private IEnumerable<Action> GetRetypedColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetRenamedColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetRenamedTables(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetNewColumns(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> results = new List<Action>();

			var schemaColumns = connection.Query<ColumnRef>(
				@"SELECT SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [TableName], [c].[Name] AS [ColumnName], [t].[object_id] AS [ObjectID]
				FROM [sys].[tables] [t] INNER JOIN [sys].[columns] [c] ON [t].[object_id]=[c].[object_id]", null);

			var dbObjects = schemaColumns.GroupBy(item => new DbObject(item.Schema, item.TableName) { ObjectID = item.ObjectID });
			var dboDictionary = dbObjects.ToDictionary(obj => obj.Key, obj => obj.Key.ObjectID);

			Dictionary<DbObject, Type> modelTypeDict = new Dictionary<DbObject, Type>();

			var modelColumns = modelTypes.SelectMany(mt => mt.GetProperties().Select(pi =>
			{
				DbObject obj = DbObject.FromType(mt);
				modelTypeDict.Add(obj, mt);
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
				if (IsTableEmpty(connection, colGroup.Key.Schema, colGroup.Key.Name))
				{
					results.Add(new DropTable(colGroup.Key, dboDictionary[colGroup.Key], connection));
					results.Add(new CreateTable(modelTypeDict[colGroup.Key]));
				}
				else
				{
					results.Add(new AddColumns(colGroup));
				}				
			}

			return results;
		}

		private bool IsTableEmpty(IDbConnection connection, string schema, string tableName)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{schema}].[{tableName}]", null) ?? 0) == 0);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var action in _actions)
			{
				foreach (var cmd in action.SqlCommands())
				{
					sb.Append(cmd);
					sb.AppendLine("GO");
					sb.AppendLine();
				}				
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
