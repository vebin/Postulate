using Dapper;
using Postulate.Attributes;
using System;

namespace BlobBackupLib.Queries
{
	public class LatestVersionsResult
	{
		public string Container { get; set; }
		public string RestoreName { get; set; }
		public string BackupName { get; set; }
		public int Version { get; set; }
		public DateTime? DateModified { get; set; }
		public DateTime DateArchived { get; set; }
		public long Size { get; set; }
	}

	public class LatestVersions : Query<LatestVersionsResult>
	{
		public LatestVersions(int accountId) : base(
			@"SELECT 
				[b].[Container], [b].[Path] AS [RestoreName], [v].[Name] AS [BackupName],  [v].[Number] AS [Version], 
				[v].[DateModified], [v].[DateArchived], [v].[Size]
			FROM [dbo].[Blob] [b] INNER JOIN [dbo].[Version] [v] ON [b].[LatestVersionID]=[v].[Id]
			WHERE [b].[AccountId]=@accountId")
		{
			_builtInParams.Add("accountId", accountId);
		}

		[QueryField("[b].[Path] LIKE @filename")]
		public string Filename { get; set; }

		[QueryField("[v].[DateArchived]>=@FromArchiveDate")]
		public DateTime? FromDateArchived { get; set; }

		[QueryField("[v].[DateArchived]<=@ToArchiveDate")]
		public DateTime? ToDateArchived { get; set; }

		[QueryField("[v].[DateModified]>=@FromModifiedDate")]
		public DateTime? FromModifiedDate { get; set; }

		[QueryField("[v].[DateModified]<=@ToDateModified")]
		public DateTime? ToDateModified { get; set; }
	}
}
