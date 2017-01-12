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
	}
}
