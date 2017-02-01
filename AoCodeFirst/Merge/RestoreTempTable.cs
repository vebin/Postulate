using System;
using System.Collections.Generic;
using System.Linq;
using Postulate.Extensions;
using Postulate.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Dapper;

namespace Postulate.Merge
{
	class RestoreTempTable : SchemaMerge.Action
	{
		private readonly DbObject _tempTable;
		private readonly DbObject _modelTable;
		private readonly Type _modelType;
		private readonly Dictionary<string, string> _addColumns;

		public RestoreTempTable(DbObject tempTable, Type modelType, IDbConnection connection, Dictionary<string, string> addColumns = null) : base(MergeObjectType.Table, MergeActionType.Load, $"Restore {tempTable.QualifiedName()}")
		{
			_tempTable = tempTable;
			_modelTable = DbObject.FromTempName(tempTable);
			_modelType = modelType;
			_addColumns = addColumns;
			if (addColumns == null) _addColumns = NullMissingTempColumns(connection);
		}

		private Dictionary<string, string> NullMissingTempColumns(IDbConnection connection)
		{
			var tempColumns = connection.Query<string>(
				"SELECT [name] FROM [sys].[columns] WHERE [object_id]=@objId", new { objId = _tempTable.ObjectID });

			return ModelColumnNames()
				.Where(col => !tempColumns.Any(tcol => col.ToLower().Equals(tcol.ToLower())))
				.ToDictionary(item => item, item => "NULL");
		}

		public override IEnumerable<string> SqlCommands()
		{
			yield return $"SET IDENTITY_INSERT {_modelTable} ON";

			var insertColumns = ModelColumnNames()
				.WhereNotIn(_addColumns.Select(kp => kp.Key))
				.Select(col => $"[{col}]")
				.Concat(_addColumns.Select(kp => $"[{kp.Key}]"));

			var selectColumns = ModelColumnNames()
				.WhereNotIn(_addColumns.Select(kp => kp.Key))
				.Select(col => $"[{col}]")
				.Concat(_addColumns.Select(kp => kp.Value));

			yield return $"INSERT INTO {_modelTable} (\r\n\t" +
				$"{string.Join(", ", insertColumns)}\r\n" +
				$") SELECT {string.Join(", ", selectColumns)}\r\n" +
				$"FROM {_tempTable}";

			yield return $"SET IDENTITY_INSERT {_modelTable} OFF";

			yield return $"DROP TABLE {_tempTable}";
		}

		private IEnumerable<string> ModelColumnNames()
		{
			return _modelType.GetProperties().Where(pi => 
				!pi.HasAttribute<CalculatedAttribute>() &&
				!pi.HasAttribute<NotMappedAttribute>() &&
				CreateTable.IsSupportedType(pi.PropertyType)).Select(pi => pi.SqlColumnName());
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}

		public class TempTableRef
		{
			public string Schema { get; set; }
			public string TempName { get; set; }
			public string ModelName { get; set; }
			public int TempObjectId { get; set; }

			public DbObject ModelObject()
			{
				return new DbObject(Schema, ModelName);
			}

			public DbObject TempObject()
			{
				return new DbObject(Schema, TempName);
			}
		}
	}
}
