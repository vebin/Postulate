using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Models
{
	internal class ChangeHistoryRecord<TKey>
	{
		public TKey RecordId { get; set; }
		public int Version { get; set; }
		public DateTime DateTime { get; set; }
		public string ColumnName { get; set; }
		public string OldValue { get; set; }
		public string NewValue { get; set; }
	}
}
