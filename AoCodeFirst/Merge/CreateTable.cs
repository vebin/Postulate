using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	internal class CreateTable : SchemaMerge.Action
	{
		private readonly Type _modelType;

		public CreateTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Create, DbObject.FromType(modelType).QualifiedName())
		{
			_modelType = modelType;
		}

		public static Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0)
		{
			return new Dictionary<Type, string>()
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
		}

		public static bool IsSupportedType(Type type)
		{
			return 
				SupportedTypes().ContainsKey(type) ||
				(type.IsEnum && type.GetEnumUnderlyingType().Equals(typeof(int))) ||
				(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSupportedType(type.GetGenericArguments()[0]));
		}

		public override IEnumerable<string> SqlCommands()
		{
			yield return
				$"CREATE TABLE {DbObject.SqlServerName(_modelType)} (\r\n\t" +
					string.Join(",\r\n\t", CreateTableMembers(false)) +
				"\r\n)";
		}

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

			results.AddRange(_modelType.GetProperties().Where(pi => pi.HasAttribute<Attributes.ForeignKeyAttribute>())
				.Select(pi =>
				{
					var fk = pi.GetCustomAttribute<Attributes.ForeignKeyAttribute>();
					return
						$@"CONSTRAINT [{pi.ForeignKeyName()}] FOREIGN KEY (
							[{pi.SqlColumnName()}]
						) REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (
							[{nameof(DataRecord<int>.Id)}]
						)";
				}));

			results.AddRange(_modelType.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
				.Where(attr => HasColumnName(_modelType, attr.ColumnName))
				.Select(fk =>
				{
					return
						$@"CONSTRAINT [FK_{DbObject.ConstraintName(_modelType)}_{fk.ColumnName}] FOREIGN KEY (
							[{fk.ColumnName}]
						) REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (
							[{nameof(DataRecord<int>.Id)}]
						)";
				}));

			return results;
		}

		public static bool HasColumnName(Type modelType, string columnName)
		{
			return modelType.GetProperties().Any(pi => pi.SqlColumnName().ToLower().Equals(columnName.ToLower()));
		}

		private IEnumerable<string> CreateTableUniqueConstraints()
		{
			List<string> results = new List<string>();

			if (PrimaryKeyColumns(markedOnly:true).Any())
			{
				results.Add($"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_Id] UNIQUE ([Id])");
			}

			results.AddRange(_modelType.GetProperties().Where(pi =>
			{
				var unique = pi.GetCustomAttribute<UniqueKeyAttribute>();
				return (unique != null);
			}).Select(pi =>
			{
				return $"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_{pi.SqlColumnName()}] UNIQUE ([{pi.SqlColumnName()}])";
			}));

			results.AddRange(_modelType.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
			{
				string constrainName = (string.IsNullOrEmpty(u.ConstraintName)) ? $"U_{DbObject.ConstraintName(_modelType)}_{i}" : u.ConstraintName;
				return $"CONSTRAINT [{constrainName}] UNIQUE ({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
			}));

			return results;
		}

		protected IEnumerable<string> PrimaryKeyColumns(bool markedOnly = false)
		{
			return PrimaryKeyColumns(_modelType, markedOnly);
		}

		public static IEnumerable<string> PrimaryKeyColumns(Type modelType, bool markedOnly = false)
		{
			var pkColumns = modelType.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>()).Select(pi => pi.SqlColumnName());

			if (pkColumns.Any() || markedOnly) return pkColumns;

			return new string[] { modelType.IdentityColumnName() };
		}

		private string CreateTablePrimaryKey()
		{
			return $"CONSTRAINT [PK_{DbObject.ConstraintName(_modelType)}] PRIMARY KEY ({string.Join(", ", PrimaryKeyColumns().Select(col => $"[{col}]"))})";
		}

		private IEnumerable<string> CreateTableColumns()
		{
			List<string> results = new List<string>();

			Position identityPos = Position.StartOfTable;
			var ip = _modelType.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip == null) ip = _modelType.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip != null) identityPos = ip.Position;

			if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql());

			results.AddRange(_modelType.GetProperties()
				.Where(p => 
					p.CanWrite && 
					!p.Name.ToLower().Equals(nameof(DataRecord<int>.Id).ToLower()) &&
					!p.HasAttribute<NotMappedAttribute>())
				.Select(pi => 
					{
						CalculatedAttribute calc;
						if (pi.HasAttribute(out calc))
						{
							return $"[{pi.SqlColumnName()}] AS {calc.Expression}";
						}
						else
						{
							return $"[{pi.SqlColumnName()}] {pi.SqlColumnType()}{pi.SqlDefaultExpression(forCreateTable: true)}";
						}						
					}));

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

			Type keyType = FindKeyType(_modelType);

			return $"[{_modelType.IdentityColumnName()}] {typeMap[keyType]}";
		}

		private Type FindKeyType(Type modelType)
		{
			if (!modelType.IsDerivedFromGeneric(typeof(DataRecord<>))) throw new ArgumentException("Model class must derive from DataRecord<TKey>");

			Type checkType = modelType;
			while (!checkType.IsGenericType) checkType = checkType.BaseType;
			return checkType.GetGenericArguments()[0];			
		}

		public override IEnumerable<string> ValidationErrors()
		{
			foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<PrimaryKeyAttribute>())))
			{
				if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Primary key column [{pi.Name}] may not use MAX size.";
				if (pi.PropertyType.IsNullableGeneric()) yield return $"Primary key column [{pi.Name}] may not be nullable.";
			}
			
			foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<UniqueKeyAttribute>())))
			{
				if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
			}

			// class-level unique with MAX
			var uniques = _modelType.GetCustomAttributes<UniqueKeyAttribute>();
			foreach (var u in uniques)
			{
				foreach (var col in u.ColumnNames)
				{
					PropertyInfo pi = _modelType.GetProperty(col);
					if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
				}
			}
		}
	}
}
