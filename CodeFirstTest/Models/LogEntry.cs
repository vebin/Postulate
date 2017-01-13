using Postulate;
using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	[IdentityPosition(Position.EndOfTable)]
	public class LogEntry : DataRecord<Guid>
	{
		public DateTime DateTime { get; set; }
		public string Description { get; set; }

		public static SqlServerRowManager<LogEntry, Guid> Db()
		{
			return new SqlServerRowManager<LogEntry, Guid>(new PostulateDb());
		}
	}
}
