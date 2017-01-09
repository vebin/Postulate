using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	/// <summary>
	/// Indicates that the column holds a large value, and should be excluded from queries executed by RowManagerBase.Query method for performance reasons
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class LargeValueColumn : Attribute
	{
	}
}
