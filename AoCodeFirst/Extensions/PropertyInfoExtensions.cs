using Postulate.Attributes;
using Postulate.Merge;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Linq;
using Postulate.Enums;
using System.Data;

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
			if (t.IsEnum) t = t.GetEnumUnderlyingType();

			return $"{typeMap[t]} {nullable}";
		}

		public static string SqlDefaultExpression(this PropertyInfo propertyInfo, bool forCreateTable = false)
		{
			string template = (forCreateTable) ? " DEFAULT ({0})" : "{0}";

			DefaultExpressionAttribute def;
			if (propertyInfo.DeclaringType.HasAttribute(out def) && propertyInfo.Name.Equals(def.ColumnName)) return string.Format(template, Quote(propertyInfo, def.Expression));
			if (propertyInfo.HasAttribute(out def)) return string.Format(template, Quote(propertyInfo, def.Expression));

			InsertExpressionAttribute ins;
			if (propertyInfo.HasAttribute(out ins) && !ins.HasParameters) return string.Format(template, Quote(propertyInfo, ins.Expression));

			// if the expression is part of a CREATE TABLE statement, it's not necessary to go any further
			if (forCreateTable) return null;

			if (propertyInfo.AllowSqlNull()) return "NULL";
			
			throw new Exception($"{propertyInfo.DeclaringType.Name}.{propertyInfo.Name} property does not have a [DefaultExpression] nor [InsertExpression] attribute with no parameters.");
		}

		private static string Quote(PropertyInfo propertyInfo, string expression)
		{
			string result = expression;

			var quotedTypes = new Type[] { typeof(string), typeof(DateTime) };
			if (quotedTypes.Any(t => t.Equals(propertyInfo.PropertyType)))
			{
				if (result.Contains("'") && !result.StartsWith("'") && !result.EndsWith("'")) result = result.Replace("'", "''");
				if (!result.StartsWith("'")) result = "'" + result;
				if (!result.EndsWith("'")) result = result + "'";
			}

			return result;
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

		public static bool HasSaveAction(this PropertyInfo propertyInfo, SaveAction action)
		{
			if (action == SaveAction.NotSet) return true;

			if (propertyInfo.HasAttributeWhere<ColumnAccessAttribute>(attr => attr.Access == Access.InsertOnly) && action == SaveAction.Update) return false;

			if (propertyInfo.HasAttributeWhere<ColumnAccessAttribute>(attr => attr.Access == Access.UpdateOnly) && action == SaveAction.Insert) return false;

			return true;
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
