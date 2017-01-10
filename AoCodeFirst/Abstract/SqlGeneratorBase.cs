using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Abstract
{
	public abstract class SqlGeneratorBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{		
		protected const string _idColumn = "ID";

		private bool _squareBraces = false;
		private string _defaultSchema = string.Empty;

		public SqlGeneratorBase(bool squareBraces = false, string defaultSchema = "")
		{
			_squareBraces = squareBraces;
			_defaultSchema = defaultSchema;
		}

		public string TableName(Type tableType = null)
		{
			Type t = (tableType == null) ? typeof(TRecord) : tableType;
			string result = t.Name;

			TableAttribute attr = t.GetCustomAttribute<TableAttribute>();
			if (attr != null)
			{
				if (!string.IsNullOrEmpty(attr.Schema)) result = $"{attr.Schema}.";
				result += attr.Name;
			}
			else
			{
				if (!string.IsNullOrEmpty(_defaultSchema))
				{
					result = $"{_defaultSchema}.{t.Name}";
				}
			}

			if (_squareBraces) result = string.Join(".", result.Split('.').Select(s => $"[{s}]"));

			return result;
		}

		private string TableConstraintName()
		{
			Type t = typeof(TRecord);
			string result = t.Name;

			var attr = t.GetCustomAttribute<TableAttribute>();
			if (attr != null)
			{
				result = (!string.IsNullOrEmpty(attr.Schema) ? attr.Schema : string.Empty) + attr.Name;
			}

			return result;
		}

		private string[] GetWriteableColumns(AccessOption option)
		{
			return GetWriteableProperties(option).Select(p => p.Name).ToArray();			
		}

		private IEnumerable<PropertyInfo> GetWriteableProperties(AccessOption option)
		{
			Type t = typeof(TRecord);
			return t.GetProperties().Where(p => AllowAccess(p, option));
		}

		public string[] SelectableColumns(bool allColumns, bool squareBraces = false)
		{
			Type t = typeof(TRecord);
			var props = t.GetProperties().Where(p => p.CanRead);
			var results = (allColumns) ?
				props.Select(p => ColumnName(p)) :
				props.Where(p => p.GetCustomAttribute<LargeValueColumn>() == null).Select(p => ColumnName(p));

			if (squareBraces) results = results.Select(p => $"[{p}]");

			return results.ToArray();
		}

		public string[] InsertColumns()
		{
			var result = GetWriteableColumns(AccessOption.InsertOnly);
			if (_squareBraces) return result.Select(s => $"[{s}]").ToArray();
			return result;
		}

		public string[] InsertExpressions()
		{			
			return GetWriteableProperties(AccessOption.InsertOnly).Select(pi =>
			{
				var expr = pi.GetCustomAttribute<InsertExpressionAttribute>() as InsertExpressionAttribute;
				if (expr != null) return expr.Expression;
				return $"@{pi.Name}";
			}).ToArray();
		}

		public string[] UpdateExpressions()
		{
			return GetWriteableProperties(AccessOption.UpdateOnly).Select(pi =>
			{
				string result = (_squareBraces) ? $"[{ColumnName(pi)}]" : ColumnName(pi);
				var expr = pi.GetCustomAttributes(typeof(UpdateExpressionAttribute)).FirstOrDefault() as UpdateExpressionAttribute;
				if (expr != null) return result += $"={expr.Expression}";
				return result += $"=@{pi.Name}";
			}).ToArray();			
		}

		public abstract string FindStatement();

		public abstract string SelectStatement(bool allColumns = true);

		public abstract string UpdateStatement();

		public abstract string InsertStatement();

		public abstract string DeleteStatement();

		public abstract string CreateTableStatement(bool withForeignKeys = false);

		public string[] CreateTableMembers(bool withForeignKeys = false)
		{
			List<string> results = new List<string>();					

			results.AddRange(CreateTableColumns());
			
			results.Add(CreateTablePrimaryKey());

			results.AddRange(CreateTableUniqueConstraints());

			if (withForeignKeys) results.AddRange(CreateTableForeignKeys());

			return results.ToArray();
		}

		private IEnumerable<string> CreateTableForeignKeys()
		{
			List<string> results = new List<string>();

			Type t = typeof(TRecord);
			string openBrace = (_squareBraces) ? "[" : string.Empty;
			string closeBrace = (_squareBraces) ? "]" : string.Empty;

			results.AddRange(t.GetProperties().Where(pi =>
			{
				var fk = pi.GetCustomAttribute<Attributes.ForeignKeyAttribute>();
				return (fk != null);
			}).Select(pi =>
			{
				var fk = pi.GetCustomAttribute<Attributes.ForeignKeyAttribute>();
				return 
					$@"CONSTRAINT {openBrace}FK_{TableConstraintName()}_{ColumnName(pi)}{closeBrace} FOREIGN KEY (
						{openBrace}{ColumnName(pi)}{closeBrace}
					) REFERENCES {TableName(fk.PrimaryTableType)} (
						{openBrace}{_idColumn}{closeBrace}
					)";
			}));

			results.AddRange(t.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
				.Where(attr => HasColumnName(t, attr.ColumnName))
				.Select(fk =>
				{
					return
						$@"CONSTRAINT {openBrace}FK_{TableConstraintName()}_{fk.ColumnName}{closeBrace} FOREIGN KEY (
							{openBrace}{fk.ColumnName}{closeBrace}
						) REFERENCES {TableName(fk.PrimaryTableType)} (
							{openBrace}{_idColumn}{closeBrace}
						)";
				}));

			return results;
		}

		private bool HasColumnName(Type t, string columnName)
		{
			return t.GetProperties().Any(pi => pi.Name.ToLower().Equals(columnName.ToLower()));
		}

		private IEnumerable<string> CreateTableUniqueConstraints()
		{
			List<string> results = new List<string>();

			Type t = typeof(TRecord);
			string openBrace = (_squareBraces) ? "[" : string.Empty;
			string closeBrace = (_squareBraces) ? "]" : string.Empty;

			results.AddRange(t.GetProperties().Where(pi =>
			{
				var unique = pi.GetCustomAttribute<UniqueKeyAttribute>();
				return (unique != null);
			}).Select(pi => 
			{
				return $"CONSTRAINT {openBrace}U_{TableConstraintName()}_{ColumnName(pi)}{closeBrace} UNIQUE ({openBrace}{ColumnName(pi)}{closeBrace})";
			}));

			results.AddRange(t.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
			{
				return $"CONSTRAINT {openBrace}U_{TableConstraintName()}_{i}{closeBrace} UNIQUE ({string.Join(", ", u.ColumnNames.Select(col => $"{openBrace}{col}{closeBrace}"))}";
			}));

			return results;
		}

		protected IEnumerable<string> PrimaryKeyColumns()
		{
			Type t = typeof(TRecord);
			var pkColumns = t.GetProperties().Where(pi =>
			{
				var pkAttr = pi.GetCustomAttribute<PrimaryKeyAttribute>();
				return (pkAttr != null);
			}).Select(pi => ColumnName(pi));

			if (pkColumns.Any()) return pkColumns;

			return new string[] { _idColumn };
		}

		private string CreateTablePrimaryKey()
		{
			string openBrace = (_squareBraces) ? "[" : string.Empty;
			string closeBrace = (_squareBraces) ? "]" : string.Empty;			
			return $"CONSTRAINT {openBrace}PK_{TableConstraintName()}{closeBrace} PRIMARY KEY ({string.Join(", ", PrimaryKeyColumns().Select(col => openBrace + col + closeBrace))})";
		}

		private IEnumerable<string> CreateTableColumns()
		{
			List<string> results = new List<string>();

			Type t = typeof(TRecord);
			string openBrace = (_squareBraces) ? "[" : string.Empty;
			string closeBrace = (_squareBraces) ? "]" : string.Empty;

			Position identityPos = Position.StartOfTable;
			var ip = t.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip == null) ip = t.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip != null) identityPos = ip.Position;

			if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql());

			results.AddRange(t.GetProperties()
				.Where(p => p.CanWrite && !p.Name.ToLower().Equals(_idColumn.ToLower()))
				.Select(pi => $"{openBrace}{ColumnName(pi)}{closeBrace} {SqlTypeFromProperty(pi)}"));

			if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql());

			return results;
		}

		private string IdentityColumnSql()
		{
			var typeMap = new Dictionary<Type, string>()
			{
				{ typeof(int), "int identity(1,1)" },
				{ typeof(long), "bigint identity(1,1)" },
				{ typeof(Guid), "uniqueidentifier DEFAULT NewSequentialID()" }
			};

			string openBrace = (_squareBraces) ? "[" : string.Empty;
			string closeBrace = (_squareBraces) ? "]" : string.Empty;

			return $"{openBrace}{_idColumn}{closeBrace} {typeMap[typeof(TKey)]}";
		}

		private static bool AllowAccess(PropertyInfo pi, AccessOption option)
		{
			if (pi.CanWrite && !pi.Name.Equals(_idColumn) && !pi.GetCustomAttributes<CalculatedAttribute>().Any())
			{				
				var attrs = pi.DeclaringType.GetCustomAttributes<ColumnAccessAttribute>();
				if (attrs != null)
				{
					var classAttr = attrs.SingleOrDefault(a => a.ColumnName.Equals(pi.Name));
					if (classAttr != null) return classAttr.Access == option;
				}

				var attr = pi.GetCustomAttribute<ColumnAccessAttribute>() as ColumnAccessAttribute;
				if (attr != null) return attr.Access == option;

				return true;
			}
			return false;
		}

		private static string ColumnName(PropertyInfo pi)
		{
			string result = pi.Name;

			var attr = pi.GetCustomAttribute<ColumnAttribute>();
			if (attr != null && !string.IsNullOrEmpty(attr.Name)) result = attr.Name;

			return result;
		}

		public static string SqlTypeFromProperty(PropertyInfo propertyInfo)
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
