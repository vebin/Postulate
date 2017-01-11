using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	public class ModelSchemaMerge
	{
		public ModelSchemaMerge(string @namespace, IDbConnection connection)
		{
			var modelTypes = Assembly.GetCallingAssembly().GetTypes()
				.Where(t => 
					t.Namespace.Equals(@namespace) && 
					!t.IsAbstract &&					
					(IsDerivedFromGeneric(t, typeof(DataRecord<>))));
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
