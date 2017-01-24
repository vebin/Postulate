using System;
using System.Collections.Generic;
using System.Reflection;
using static Postulate.Merge.AddColumns;
using Postulate.Extensions;
using Postulate.Attributes;

namespace Postulate.Merge
{
	internal class RetypeColumn : SchemaMerge.Action
	{
		private readonly PropertyInfo _modelColumn;

		public RetypeColumn(ColumnRef fromSchemaColumn, ColumnRef toModelColumn) : base(MergeObjectType.Column, MergeActionType.Retype, 
			$"{toModelColumn.ToString()}: {fromSchemaColumn.DataTypeComparison(toModelColumn)}")
		{
			_modelColumn = toModelColumn.PropertyInfo;
		}

		public override IEnumerable<string> SqlCommands()
		{
			Type modelType = _modelColumn.ReflectedType;
			DbObject obj = DbObject.FromType(modelType);

			if (_modelColumn.HasAttribute<PrimaryKeyAttribute>())
			{
				// issue #7, table must be dropped and rebuilt
			}

			yield return $"ALTER TABLE [{obj.Schema}].[{obj.Name}] ALTER COLUMN [{_modelColumn.SqlColumnName()}] {_modelColumn.SqlColumnType()}";

			// restore PK
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}
	}
}
