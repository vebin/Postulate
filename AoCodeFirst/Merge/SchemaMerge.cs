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

		public SchemaMerge(string @namespace, IDbConnection connection)
		{
			IDbConnection cn = connection;
			var modelTypes = Assembly.GetCallingAssembly().GetTypes()
				.Where(t => 
					t.Namespace.Equals(@namespace) && 
					!t.IsAbstract &&					
					(IsDerivedFromGeneric(t, typeof(DataRecord<>))));			

			GetSchemaMergeActionHandler[] methods = new GetSchemaMergeActionHandler[]
			{
				GetNewTables, GetNewForeignKeys/*, GetRenamedTables, GetDeletedTables,
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

			foreach (var a in _actions) connection.Execute(a.SqlScript());
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
			throw new NotImplementedException();
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
				sb.Append(action.SqlScript());
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

			public abstract string SqlScript();
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

		// adapted from http://stackoverflow.com/questions/17058697/determining-if-type-is-a-subclass-of-a-generic-type
		private static bool IsDerivedFromGeneric(Type type, Type genericType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
			if (type.BaseType != null) return IsDerivedFromGeneric(type.BaseType, genericType);
			return false;
		}
	}
}
