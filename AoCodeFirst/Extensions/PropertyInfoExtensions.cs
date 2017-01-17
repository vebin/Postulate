using Postulate.Attributes;
using Postulate.Merge;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Linq;

namespace Postulate.Extensions
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

		public static string SqlDefaultExpression(this PropertyInfo propertyInfo)
		{
			DefaultExpressionAttribute def;
			if (propertyInfo.HasAttribute(out def)) return def.Expression;

			InsertExpressionAttribute ins;
			if (propertyInfo.HasAttribute(out ins) && !ins.HasParameters) return ins.Expression;

			throw new Exception($"{propertyInfo.DeclaringType.Name}.{propertyInfo.Name} property does not have a [DefaultExpression] nor [InsertExpression] attribute with no parameters.");
		}

		public static Attributes.ForeignKeyAttribute GetForeignKeyAttribute(this PropertyInfo propertyInfo)
		{
			Attributes.ForeignKeyAttribute fk;
			if (propertyInfo.HasAttribute(out fk)) return fk;

			fk = propertyInfo.DeclaringType.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
				.SingleOrDefault(attr => attr.ColumnName.Equals(propertyInfo.Name));
			if (fk != null) return fk;

			throw new ArgumentException($"The property {propertyInfo.Name} does not have a [ForeignKey] attribute.");
		}

		public static string ForeignKeyName(this PropertyInfo propertyInfo)
		{
			var fk = GetForeignKeyAttribute(propertyInfo);
			return $"FK_{DbObject.ConstraintName(propertyInfo.DeclaringType)}_{propertyInfo.SqlColumnName()}";
		}

		public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider provider, out TAttribute attribute) where TAttribute : Attribute
		{
			attribute = null;
			var attrs = provider.GetCustomAttributes(typeof(TAttribute), true);
			if (attrs.Any())
			{
				attribute = attrs.First() as TAttribute;
				return true;
			}
			return false;
		}

		public static bool HasAttributeWhere<TAttribute>(this ICustomAttributeProvider provider, Func<TAttribute, bool> predicate) where TAttribute : Attribute
		{
			TAttribute attr;
			if (HasAttribute(provider, out attr)) return predicate.Invoke(attr);
			return false;
		}

		public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider provider) where TAttribute : Attribute
		{
			TAttribute attr;
			return HasAttribute(provider, out attr);
		}

		public static bool AllowSqlNull(this PropertyInfo propertyInfo)
		{
			if (InPrimaryKey(propertyInfo)) return false;
			var required = propertyInfo.GetCustomAttribute<RequiredAttribute>();
			if (required != null) return false;
			return propertyInfo.PropertyType.IsNullable();
		}

		private static bool InPrimaryKey(PropertyInfo propertyInfo)
		{
			return propertyInfo.HasAttribute<PrimaryKeyAttribute>();
		}
	}
}
