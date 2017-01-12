﻿using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	public class CreateTable : SchemaMerge.Action
	{
		private readonly Type _modelType;

		public CreateTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Create, modelType.Name)
		{
			_modelType = modelType;
		}

		public override string SqlScript()
		{
			return
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

			results.AddRange(_modelType.GetProperties().Where(pi => pi.HasAttribute<ForeignKeyAttribute>())
				.Select(pi =>
				{
					var fk = pi.GetCustomAttribute<ForeignKeyAttribute>();
					return
						$@"CONSTRAINT [{pi.ForeignKeyName()}] FOREIGN KEY (
							[{pi.SqlColumnName()}]
						) REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (
							[{nameof(DataRecord<int>.ID)}]
						)";
				}));

			results.AddRange(_modelType.GetCustomAttributes<ForeignKeyAttribute>()
				.Where(attr => HasColumnName(_modelType, attr.ColumnName))
				.Select(fk =>
				{
					return
						$@"CONSTRAINT [FK_{DbObject.ConstraintName(_modelType)}_{fk.ColumnName}] FOREIGN KEY (
							[{fk.ColumnName}]
						) REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (
							[{nameof(DataRecord<int>.ID)}]
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

			if (PrimaryKeyColumns().Any())
			{
				results.Add($"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_ID] UNIQUE ([ID])");
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
				return $"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_{i}] UNIQUE ({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
			}));

			return results;
		}

		protected IEnumerable<string> PrimaryKeyColumns()
		{			
			var pkColumns = _modelType.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>()).Select(pi => pi.SqlColumnName());

			if (pkColumns.Any()) return pkColumns;

			return new string[] { nameof(DataRecord<int>.ID) };
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
				.Where(p => p.CanWrite && !p.Name.ToLower().Equals(nameof(DataRecord<int>.ID).ToLower()))
				.Select(pi => $"[{pi.SqlColumnName()}] {pi.SqlColumnType()}"));

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

			Type keyType = (_modelType.IsGenericType) ? _modelType.GetGenericArguments()[0] : typeof(int);			

			return $"[{nameof(DataRecord<int>.ID)}] {typeMap[keyType]}";
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
