using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	internal class PrimaryKeyRef
	{
		public string Schema { get; set; }
		public string TableName { get; set; }
		public string ColumnList { get; set; }
		public Type ModelType { get; set; }
		public int ObjectId { get; set; }

		public override bool Equals(object obj)
		{
			PrimaryKeyRef test = obj as PrimaryKeyRef;
			if (test != null)
			{
				return
					test.Schema.ToLower().Equals(Schema.ToLower()) &&
					test.TableName.ToLower().Equals(TableName.ToLower()) &&
					test.ColumnList.ToLower().Equals(ColumnList.ToLower());
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Schema.GetHashCode() + TableName.GetHashCode() + ColumnList.GetHashCode();
		}
	}
}
