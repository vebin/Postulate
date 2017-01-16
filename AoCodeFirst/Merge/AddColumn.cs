using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	internal class AddColumn : SchemaMerge.Action
	{
		private readonly PropertyInfo _propertyInfo;

		public AddColumn(ColumnRef columnInfo) : base(MergeObjectType.Column, MergeActionType.Create, columnInfo.ToString())
		{
			_propertyInfo = columnInfo.PropertyInfo;
		}

		public override IEnumerable<string> SqlCommands()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<string> ValidationErrors()
		{
			return new string[] { };
		}

		internal class ColumnRef
		{
			public string Schema { get; set; }
			public string TableName { get; set; }
			public string ColumnName { get; set; }
			public PropertyInfo PropertyInfo { get; set; }

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
