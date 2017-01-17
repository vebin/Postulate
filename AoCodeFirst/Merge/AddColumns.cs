using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	internal class AddColumns : SchemaMerge.Action
	{
		private readonly IEnumerable<ColumnRef> _columns;

		public AddColumns(IEnumerable<ColumnRef> columns) : 
			base(MergeObjectType.Column, MergeActionType.Create, $"{columns.First().Schema}.{columns.First().TableName}: {string.Join(", ", columns.Select(col => col.ColumnName))}")
		{
			if (columns.GroupBy(item => new { schema = item.Schema, table = item.TableName }).Count() > 1)
			{
				throw new ArgumentException("Can't have more than one table in an AddColumns merge action.");
			}

			_columns = columns;
		}

		public override IEnumerable<string> SqlCommands()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<string> ValidationErrors()
		{
			// not nullable columns require default expression attribute
			return new string[] { };
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
