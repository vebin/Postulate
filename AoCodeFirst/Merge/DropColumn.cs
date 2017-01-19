using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Postulate.Merge.AddColumns;
using static Postulate.Merge.CreateForeignKey;
using Postulate.Extensions;
using Postulate.Attributes;
using System.Data;

namespace Postulate.Merge
{
	internal class DropColumn : SchemaMerge.Action
	{
		private readonly ColumnRef _columnRef;
		private readonly ForeignKeyRef _dropFK;
		//private readonly IEnumerable<ForeignKeyRef> _foreignKeys; you won't be dropping the key usually, so there's really no need to drop dependent FKs

		internal DropColumn(ColumnRef columnRef, IDbConnection connection) : base(MergeObjectType.Column, MergeActionType.Delete, columnRef.ToString())
		{
			_columnRef = columnRef;

			ForeignKeyRef fk;
			if (columnRef.IsForeignKey(connection, out fk)) _dropFK = fk;
			//_foreignKeys = GetReferencingForeignKeys(connection, columnRef.ObjectID);
		}

		public override IEnumerable<string> SqlCommands()
		{
			if (_dropFK != null) yield return DropFKStatement(_dropFK);

			//foreach (var fk in _foreignKeys) yield return DropFKStatement(fk);

			yield return $"ALTER TABLE [{_columnRef.Schema}].[{_columnRef.TableName}] DROP COLUMN [{_columnRef.ColumnName}]";
		}

		private string DropFKStatement(ForeignKeyRef fk)
		{
			return $"ALTER TABLE [{fk.ReferencingTable.Schema}].[{fk.ReferencingTable.Name}] DROP CONSTRAINT [{fk.ConstraintName}]";
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}
	}
}
