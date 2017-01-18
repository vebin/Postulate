using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Postulate.Extensions;
using Postulate.Attributes;
using System.Data;

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
					item => item.PropertyInfo.SqlDefaultExpression()))) yield return cmd;
			
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
			public string Schema { get; set; }
			public string TableName { get; set; }
			public string ColumnName { get; set; }
			public PropertyInfo PropertyInfo { get; set; }
			public int ObjectID { get; set; }

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
		}
	}
}
