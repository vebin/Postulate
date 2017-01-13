using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Extensions
{
	public static class TypeExtensions
	{
		public static bool IsNullable(this Type type)
		{
			return IsNullableGeneric(type) || type.Equals(typeof(string));
		}

		public static bool IsNullableGeneric(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		// adapted from http://stackoverflow.com/questions/17058697/determining-if-type-is-a-subclass-of-a-generic-type
		public static bool IsDerivedFromGeneric(this Type type, Type genericType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
			if (type.BaseType != null) return IsDerivedFromGeneric(type.BaseType, genericType);
			return false;
		}
	}
}
