using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

	public class SchemaMerge
	{
		private readonly List<Action> _actions;
		private readonly IEnumerable<Type> _modelTypes;
		private readonly IDbConnection _connection;

		public SchemaMerge(string @namespace, IDbConnection connection)
		{
			_connection = connection;
			_modelTypes = Assembly.GetCallingAssembly().GetTypes()
				.Where(t => 
					t.Namespace.Equals(@namespace) && 
					!t.IsAbstract &&					
					(IsDerivedFromGeneric(t, typeof(DataRecord<>))));

			_actions = new List<Action>();
			//_actions.AddRange()
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

		// adapted from http://stackoverflow.com/questions/17058697/determining-if-type-is-a-subclass-of-a-generic-type
		private static bool IsDerivedFromGeneric(Type type, Type genericType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
			if (type.BaseType != null) return IsDerivedFromGeneric(type.BaseType, genericType);
			return false;
		}
	}
}
