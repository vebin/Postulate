using Postulate;
using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using System;

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
