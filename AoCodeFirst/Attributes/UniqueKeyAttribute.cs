using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
	public class UniqueKeyAttribute : Attribute
	{
		private readonly string[] _columnNames;

		/// <summary>
		/// Denotes a unique constraint on a single property
		/// </summary>
		public UniqueKeyAttribute()
		{
		}

		/// <summary>
		/// At the class level, describes a unique constraint with a set of columns
		/// </summary>		
		public UniqueKeyAttribute(params string[] columnNames)
		{
			_columnNames = columnNames;
		}

		public string[] ColumnNames { get { return _columnNames; } }
	}
}
