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
		private readonly List<Action> _actions;

		public SchemaMerge(Type dbType, IDbConnection connection)
		{
			IDbConnection cn = connection;
			var modelTypes = dbType.Assembly.GetTypes()
				.Where(t =>
					!t.Name.StartsWith("<>") &&
					t.Namespace.Equals(dbType.Namespace) &&					
					!t.IsAbstract &&					
					t.IsDerivedFromGeneric(typeof(DataRecord<>)));					

			GetSchemaMergeActionHandler[] methods = new GetSchemaMergeActionHandler[]
			{
				GetDeletedTables, GetNewTables, GetNewForeignKeys/*, GetRenamedTables,
				GetNewColumns, GetRenamedColumns, GetRetypedColumns, GetDeletedColumns,
				GetNewPrimaryKeys, GetDeletedForeignKeys, GetDeletedPrimaryKeys*/
			};

			_actions = new List<Action>();
			foreach (var m in methods) _actions.AddRange(m.Invoke(modelTypes, cn));
		}

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
				foreach (var cmd in a.SqlCommands()) connection.Execute(cmd);
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

		private IEnumerable<Action> GetNewForeignKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			List<Action> actions = new List<Action>();

			foreach (var t in modelTypes)
			{
				foreach (var pi in CreateForeignKey.GetForeignKeys(t))
				{
					if (!connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = pi.ForeignKeyName() }))
					{
						actions.Add(new CreateForeignKey(pi));
					}
				}				
			}

			return actions;
		}

		private IEnumerable<Action> GetDeletedPrimaryKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<Action> GetDeletedForeignKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
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
				"SELECT SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [TableName], [object_id] AS [ObjectID] FROM [sys].[tables]");
			var deletedTables = allTables.Where(tbl => !modelTypes.Any(modelType =>
			{
				var obj = DbObject.FromType(modelType);
				return obj.Schema.Equals(tbl.Schema) && obj.Name.Equals(tbl.TableName);
			}));
							
			Func<int, DropTable.ForeignKeyRef[]> getDependentFKs = (int objectID) =>
			{				
				var dependentFKs = connection.Query(
					@"SELECT [fk].[name] AS [ConstraintName], SCHEMA_NAME([tbl].[schema_id]) AS [ReferencingSchema], [tbl].[name] AS [ReferencingTable] 
					FROM [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [tbl] ON [fk].[parent_object_id]=[tbl].[object_id] 
					WHERE [referenced_object_id]=@objID", new { objID = objectID });
				return dependentFKs.Select(fk => new DropTable.ForeignKeyRef() { ConstraintName = fk.ConstraintName, ReferencingTable = new DbObject(fk.ReferencingSchema, fk.ReferencingTable) }).ToArray();
			};

			results.AddRange(deletedTables.Select(del => new DropTable(new DbObject(del.Schema, del.TableName), getDependentFKs(del.ObjectID))));

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
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var action in _actions)
			{
				sb.Append(action.SqlCommands());
				sb.AppendLine();
				sb.AppendLine("GO");
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
