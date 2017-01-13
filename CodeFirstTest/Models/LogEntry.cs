using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	public class LogEntry : DataRecord<Guid>
	{
		public DateTime DateTime { get; set; }
		public string Description { get; set; }
	}
}
