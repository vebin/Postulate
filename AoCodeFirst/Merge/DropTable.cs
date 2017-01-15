﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	internal class DropTable : SchemaMerge.Action
	{
		private readonly DbObject _object;
		private readonly ForeignKeyRef[] _foreignKeys;

		public DropTable(DbObject @object, ForeignKeyRef[] dependentFKs) : base(MergeObjectType.Table, MergeActionType.Delete, @object.QualifiedName())
		{
			_object = @object;
			_foreignKeys = dependentFKs;
		}

		public override IEnumerable<string> SqlCommands()
		{			
			foreach (var fk in _foreignKeys)
			{
				yield return $"ALTER TABLE [{fk.ReferencingTable.Schema}].[{fk.ReferencingTable.Name}] DROP CONSTRAINT [{fk.ConstraintName}]";
			}

			yield return $"DROP TABLE [{_object.Schema}].[{_object.Name}]";
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
