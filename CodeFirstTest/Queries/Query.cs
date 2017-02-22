using BlobBackupLib.Models;
using Postulate;

namespace BlobBackupLib.Queries
{
	public class Query<TResult> : SqlServerQuery<TResult>
	{
		public Query(string sql) : base (sql, new LogDb())
		{
		}
	}
}
