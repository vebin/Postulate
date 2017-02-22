using Postulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackupLib.Models
{
	public class LogDb : SqlServerDb
	{
		public LogDb() : base("blobBackup")
		{
		}
	}
}
