using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Extensions
{
	public static class IEnumerableExtensions
	{
		public static IEnumerable<T> WhereNotIn<T>(this IEnumerable<T> list, IEnumerable<T> exclude)
		{
			return list.Where(item => !exclude.Contains(item));
		}
	}
}
