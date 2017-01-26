using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Models
{
	public class ChangeHistory<TKey>
	{
		public TKey RecordId { get; set; }
		public int Version { get; set; }
		public DateTimeOffset DateTime { get; set; }
		public IEnumerable<PropertyChange> Properties { get; set; }
	}
}
