using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate
{
	internal class ProfilerWrapper : IDisposable
	{
		private readonly Profiler _profiler;
		private readonly IDbConnection _cn;

		public ProfilerWrapper(SqlServerDb db, IDbConnection connection, CommandDefinition cmdDef)
		{
			_cn = connection;
			_profiler = db.Profiler;
			_profiler?.Start(connection, cmdDef);
		}

		public void Dispose()
		{
			_profiler?.Stop(_cn);
		}
	}
}
