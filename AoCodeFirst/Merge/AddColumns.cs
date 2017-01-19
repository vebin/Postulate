using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Postulate.Extensions;
using Postulate.Attributes;
using System.Data;
using static Postulate.Merge.CreateForeignKey;
using Dapper;

namespace Postulate.Merge
{
	internal class AddColumns : SchemaMerge.Action
	{
		private readonly IEnumerable<ColumnRef> _columns;
		private readonly Type _modelType;
		private readonly IDbConnection _connection;

		public AddColumns(Type modelType, IEnumerable<ColumnRef> columns, IDbConnection connection) : 
			base(MergeObjectType.Column, MergeActionType.Create, $"{columns.First().Schema}.{columns.First().TableName}: {string.Join(", ", columns.Select(col => col.ColumnName))}")
		{
			if (columns.GroupBy(item => new { schema = item.Schema, table = item.TableName }).Count() > 1)
			{
				throw new ArgumentException("Can't have more than one table in an AddColumns merge action.");
			}

			_columns = columns;
			_modelType = modelType;
			_connection = connection;
		}

		public override IEnumerable<string> SqlCommands()
		{
			var obj = DbObject.FromType(_modelType, _connection);
			string sourceTableName = obj.ToString();
			string tempTableName = $"[{obj.Schema}].[{obj.Name}_temp]";
			
			yield return SelectInto(obj, tempTableName);

			DropTable drop = new DropTable(obj, _connection);
			foreach (var cmd in drop.SqlCommands()) yield return cmd;

			CreateTable create = new CreateTable(_modelType);
			foreach (var cmd in create.SqlCommands()) yield return cmd;

			foreach (var cmd in InsertInto(tempTableName, sourceTableName, 
				_columns.ToDictionary(
					item => item.PropertyInfo.SqlColumnName(),
					item => item.PropertyInfo.SqlDefaultExpression(forCreateTable:false)))) yield return cmd;
			
			yield return $"DROP TABLE {tempTableName}";
		}

		private IEnumerable<string> InsertInto(string sourceTable, string targetTable, Dictionary<string, string> addColumns)
		{
			yield return $"SET IDENTITY_INSERT {targetTable} ON";

			var insertColumns = ModelColumnNames()
				.WhereNotIn(addColumns.Select(kp => kp.Key))
				.Select(col => $"[{col}]")
				.Concat(addColumns.Select(kp => $"[{kp.Key}]"));

			var selectColumns = ModelColumnNames()
				.WhereNotIn(addColumns.Select(kp => kp.Key))
				.Select(col => $"[{col}]")
				.Concat(addColumns.Select(kp => kp.Value));

			yield return $"INSERT INTO {targetTable} (\r\n\t" +
				$"{string.Join(", ", insertColumns)}\r\n" +
				$") SELECT {string.Join(", ", selectColumns)}\r\n" +
				$"FROM {sourceTable}";

			yield return $"SET IDENTITY_INSERT {targetTable} OFF";
		}

		private IEnumerable<string> ModelColumnNames()
		{			
			return _modelType.GetProperties().Select(pi => pi.SqlColumnName());
		}

		private string SelectInto(DbObject sourceObject, string intoTable)
		{
			return $"SELECT * INTO {intoTable} FROM {sourceObject.ToString()}";
		}

		public override IEnumerable<string> ValidationErrors()
		{			
			foreach (var col in _columns)
			{
				if (!col.PropertyInfo.AllowSqlNull() && 
					(!col.PropertyInfo.HasAttribute<DefaultExpressionAttribute>() || col.PropertyInfo.HasAttributeWhere<InsertExpressionAttribute>(a => a.HasParameters)))
				{
					yield return $"Column {col.Schema}.{col.TableName}.{col.ColumnName} must have either a [DefaultExpression] attribute or an [InsertExpression] with HasParameters = false";
				}
			}
		}

		internal class ColumnRef
		{			
			public ColumnRef(PropertyInfo pi)
			{
				PropertyInfo = pi;
				DbObject obj = DbObject.FromType(pi.ReflectedType);
				Schema = obj.Schema;
				TableName = obj.Name;
				ColumnName = pi.SqlColumnName();
			}

			public ColumnRef()
			{
			}

			public string Schema { get; set; }
			public string TableName { get; set; }
			public string ColumnName { get; set; }
			public PropertyInfo PropertyInfo { get; set; }
			public int ObjectID { get; set; }

			public string DataType { get; set; }
			public int ByteLength { get; set; }
			public int Precision { get; set; }
			public int Scale { get; set; }
			public bool IsNullable { get; set; }

			public string Length
			{
				get
				{
					if (ByteLength < 0) return "max";
					int result = ByteLength;
					if (DataType.ToLower().StartsWith("nvar")) result = result / 2;
					return $"{result}";
				}
			}

			public override bool Equals(object obj)
			{
				ColumnRef test = obj as ColumnRef;
				if (test != null)
				{
					return
						test.Schema.ToLower().Equals(this.Schema.ToLower()) &&
						test.TableName.ToLower().Equals(this.TableName.ToLower()) &&
						test.ColumnName.ToLower().Equals(this.ColumnName.ToLower());
				}
				return false;
			}

			public override int GetHashCode()
			{
				return Schema.GetHashCode() + TableName.GetHashCode() + ColumnName.GetHashCode();
			}

			public override string ToString()
			{
				return $"{Schema}.{TableName}.{ColumnName}";
			}

			public string DataTypeSyntax()
			{
				string result = null;
				switch (DataType)
				{
					case "nvarchar":
					case "char":
					case "varchar":
					case "binary":
					case "varbinary":
						result = $"{DataType}({Length})";
						break;

					case "decimal":
						result = $"{DataType}({Precision}, {Scale})";
						break;

					default:
						result = DataType;
						break;
				}

				result += (IsNullable) ? " NULL" : " NOT NULL";

				return result;
			}

			public string DataTypeComparison(ColumnRef columnRef)
			{
				return $"{this.DataTypeSyntax()} -> {columnRef.PropertyInfo.SqlColumnType()}";
			}

			internal bool IsForeignKey(IDbConnection connection, out ForeignKeyRef fk)
			{
				fk = null;
				var result = connection.QueryFirstOrDefault(
					@"SELECT 
						[fk].[name] AS [ConstraintName], [t].[name] AS [TableName], SCHEMA_NAME([t].[schema_id]) AS [Schema]
					FROM 
						[sys].[foreign_key_columns] [fkcol] INNER JOIN [sys].[columns] [col] ON 
							[fkcol].[parent_object_id]=[col].[object_id] AND
							[fkcol].[parent_column_id]=[col].[column_id]
						INNER JOIN [sys].[foreign_keys] [fk] ON [fkcol].[constraint_object_id]=[fk].[object_id]
						INNER JOIN [sys].[tables] [t] ON [fkcol].[parent_object_id]=[t].[object_id]
					WHERE
						SCHEMA_NAME([t].[schema_id])=@schema AND
						[t].[name]=@tableName AND
						[col].[name]=@columnName", new { schema = this.Schema, tableName = this.TableName, columnName = this.ColumnName });
				
				if (result != null)
				{
					fk = new ForeignKeyRef() { ConstraintName = result.ConstraintName, ReferencingTable = new DbObject(result.Schema, result.TableName) };
					return true;
				}

				return false;				
			}
		}
	}
}
