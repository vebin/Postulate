using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
	public class ForeignKeyAttribute : Attribute
	{
		private readonly string _columnName;
		private readonly Type _primaryTable;
		private readonly bool _cascadeDelete;

		/// <summary>
		/// At the class level, denotes a foreign key applied to a column with a given name
		/// </summary>
		public ForeignKeyAttribute(string columnName, Type primaryTable, bool cascadeDelete = false)
		{
			_columnName = columnName;
			_primaryTable = primaryTable;
			_cascadeDelete = false;
		}

		/// <summary>
		/// On a single property, denotes a foreign key
		/// </summary>		
		public ForeignKeyAttribute(Type primaryTable)
		{
			_primaryTable = primaryTable;
		}

		public string ColumnName { get { return _columnName; } }

		public Type PrimaryTableType { get { return _primaryTable; } }

		public bool CascadeDelete { get { return _cascadeDelete; } }
	}
}
