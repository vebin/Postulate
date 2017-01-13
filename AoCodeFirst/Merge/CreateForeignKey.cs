using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Postulate.Extensions;
using Postulate.Attributes;
using Postulate.Abstract;

namespace Postulate.Merge
{
	internal class CreateForeignKey : SchemaMerge.Action
	{
		private readonly PropertyInfo _pi;

		public CreateForeignKey(PropertyInfo propertyInfo) : base(MergeObjectType.ForeignKey, MergeActionType.Create, propertyInfo.ForeignKeyName())
		{
			_pi = propertyInfo;
		}

		public override string SqlScript()
		{
			ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
			return
				$"ALTER TABLE {DbObject.SqlServerName(_pi.DeclaringType)} ADD CONSTRAINT [{_pi.ForeignKeyName()}] FOREIGN KEY (\r\n" +
					$"\t[{_pi.SqlColumnName()}]\r\n" +
				$") REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (\r\n" +
					$"\t[{nameof(DataRecord<int>.ID)}]\r\n" +
				")";
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
				yield return modelType.GetProperty(attr.ColumnName);				
			}			
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}
	}
}
