﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Postulate.Extensions;
using Postulate.Attributes;
using Postulate.Abstract;
using Dapper;
using System.Data;

namespace Postulate.Merge
{
	internal class CreateForeignKey : SchemaMerge.Action
	{
		private readonly PropertyInfo _pi;

		public CreateForeignKey(PropertyInfo propertyInfo) : base(MergeObjectType.ForeignKey, MergeActionType.Create, propertyInfo.ForeignKeyName())
		{
			_pi = propertyInfo;
		}

		public override IEnumerable<string> SqlCommands()
		{
			ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
			string cascadeDelete = (fk.CascadeDelete) ? " ON DELETE CASCADE" : string.Empty;
			yield return
				$"ALTER TABLE {DbObject.SqlServerName(_pi.DeclaringType)} ADD CONSTRAINT [{_pi.ForeignKeyName()}] FOREIGN KEY (\r\n" +
					$"\t[{_pi.SqlColumnName()}]\r\n" +
				$") REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (\r\n" +
					$"\t[{fk.PrimaryTableType.IdentityColumnName()}]\r\n" +
				")" + cascadeDelete;
		}

		public static IEnumerable<PropertyInfo> GetForeignKeys(Type modelType)
		{
			foreach (var pi in modelType.GetProperties().Where(pi => pi.HasAttribute<ForeignKeyAttribute>()))
			{
				yield return pi;
			}

			foreach (var attr in modelType.GetCustomAttributes<ForeignKeyAttribute>()
				.Where(attr => CreateTable.HasColumnName(modelType, attr.ColumnName)))
			{
				PropertyInfo pi = modelType.GetProperty(attr.ColumnName);
				if (pi != null) yield return pi;				
			}			
		}

		internal static IEnumerable<ForeignKeyRef> GetReferencingForeignKeys(Type modelType, IEnumerable<Type> allTypes)
		{
			return allTypes.SelectMany(t => GetForeignKeys(t).Where(pi =>
			{
				ForeignKeyAttribute fk = pi.GetForeignKeyAttribute();
				return (fk.PrimaryTableType.Equals(modelType));
			}).Select(pi => 
				new ForeignKeyRef() { ConstraintName = pi.ForeignKeyName(), ReferencingTable = DbObject.FromType(pi.DeclaringType) }
			));
		}

		internal static IEnumerable<ForeignKeyRef> GetReferencingForeignKeys(IDbConnection cn, int objectID)
		{
			return cn.Query(
				@"SELECT [fk].[name] AS [ConstraintName], SCHEMA_NAME([tbl].[schema_id]) AS [ReferencingSchema], [tbl].[name] AS [ReferencingTable] 
				FROM [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [tbl] ON [fk].[parent_object_id]=[tbl].[object_id] 
				WHERE [referenced_object_id]=@objID", new { objID = objectID })
				.Select(fk => new ForeignKeyRef() { ConstraintName = fk.ConstraintName, ReferencingTable = new DbObject(fk.ReferencingSchema, fk.ReferencingTable) });
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}

		internal class ForeignKeyRef
		{
			public DbObject ReferencingTable { get; set; }
			public string ConstraintName { get; set; }
		}
	}
}
