using Postulate.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	public static class PropertyInfoExtensions
	{
		public static string SqlColumnName(this PropertyInfo propertyInfo)
		{
			string result = propertyInfo.Name;

			var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
			if (attr != null && !string.IsNullOrEmpty(attr.Name)) result = attr.Name;

			return result;
		}

		public static string SqlColumnType(this PropertyInfo propertyInfo)
		{
			string nullable = ((AllowSqlNull(propertyInfo)) ? "NULL" : "NOT NULL");

			var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>() as ColumnAttribute;
			if (attr != null && !string.IsNullOrEmpty(attr.TypeName)) return $"{attr.TypeName} {nullable}";

			string length = "max";
			var maxLenAttr = propertyInfo.GetCustomAttribute<MaxLengthAttribute>();
			if (maxLenAttr != null) length = maxLenAttr.Length.ToString();

			byte precision = 5, scale = 2; // some aribtrary defaults
			var dec = propertyInfo.GetCustomAttribute<DecimalPrecisionAttribute>();
			if (dec != null)
			{
				precision = dec.Precision;
				scale = dec.Scale;
			}

			var typeMap = new Dictionary<Type, string>()
			{
				{ typeof(string), $"nvarchar({length})" },
				{ typeof(bool), "bit" },
				{ typeof(int), "int" },
				{ typeof(decimal), $"decimal({precision}, {scale})" },
				{ typeof(double), "float" },
				{ typeof(float), "float" },
				{ typeof(long), "bigint" },
				{ typeof(short), "smallint" },
				{ typeof(byte), "tinyint" },
				{ typeof(Guid), "uniqueidentifier" },
				{ typeof(DateTime), "datetime" },
				{ typeof(TimeSpan), "time" },
				{ typeof(char), "nchar(1)" }
			};

			Type t = propertyInfo.PropertyType;
			if (t.IsGenericType) t = t.GenericTypeArguments[0];

			return $"{typeMap[t]} {nullable}";
		}

		private static bool AllowSqlNull(PropertyInfo propertyInfo)
		{
			if (InPrimaryKey(propertyInfo)) return false;
			var required = propertyInfo.GetCustomAttribute<RequiredAttribute>();
			if (required != null) return false;
			return IsTypeNullable(propertyInfo.PropertyType);
		}

		private static bool InPrimaryKey(PropertyInfo propertyInfo)
		{
			var pk = propertyInfo.GetCustomAttribute<PrimaryKeyAttribute>();
			return (pk != null);
		}

		private static bool IsTypeNullable(Type t)
		{
			return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) || t.Equals(typeof(string));
		}
	}
}
