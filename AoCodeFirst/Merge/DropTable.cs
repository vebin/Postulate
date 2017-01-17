using System.Collections.Generic;
using System.Data;
using static Postulate.Merge.CreateForeignKey;

namespace Postulate.Merge
{
	internal class DropTable : SchemaMerge.Action
	{
		private readonly DbObject _object;
		private readonly IEnumerable<ForeignKeyRef> _foreignKeys;

		public DropTable(DbObject @object, int objectID, IDbConnection connection) : this(@object, connection)
		{			
			@object.ObjectID = objectID;
		}

		public DropTable(DbObject @object, IDbConnection connection) : base(MergeObjectType.Table, MergeActionType.Delete, @object.QualifiedName())
		{
			_object = @object;
			_foreignKeys = GetReferencingForeignKeys(connection, @object.ObjectID);
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
	}
}
