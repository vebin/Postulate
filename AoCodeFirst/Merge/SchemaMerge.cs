using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

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
				GetNewTables, GetNewForeignKeys, GetRenamedTables, GetDeletedTables,
				GetNewColumns, GetRenamedColumns, GetRetypedColumns, GetDeletedColumns,
				GetNewPrimaryKeys, GetDeletedForeignKeys, GetDeletedPrimaryKeys
			};

			_actions = new List<Action>();
			foreach (var m in methods) _actions.AddRange(m.Invoke(modelTypes, cn));
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

		private IEnumerable<Action> GetNewForeignKeys(IEnumerable<Type> modelTypes, IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var action in _actions) sb.Append(action.SqlScript());
			return sb.ToString();
		}

		public abstract class Action
		{
			private readonly MergeObjectType _objectType;
			private readonly MergeActionType _actionType;

			public Action(MergeObjectType objectType, MergeActionType actionType)
			{
				_objectType = objectType;
				_actionType = actionType;
			}

			public MergeObjectType ObjectType { get { return _objectType; } }
			public MergeActionType ActionType { get { return _actionType; } }

			public abstract string SqlScript();
		}

		private IEnumerable<Action> GetNewTables(IEnumerable<Type> modelTypes, IDbConnection cn)
		{
			List<Action> actions = new List<Action>();

			foreach (var type in modelTypes)
			{
				DbObject obj = DbObject.FromType(type);
				if (!cn.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = obj.Schema, name = obj.Name }))
				{
					actions.Add(new CreateTable(type));
				}
			}

			return actions;
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
