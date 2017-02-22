using BlobBackupLib.Queries;
using CodeFirstTest.Models;
using Ginseng.Models;
using Postulate.Merge;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace CodeFirstTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var query = new LatestVersions(1);
			query.Filename = "%shared%";			
			var results = query.Execute();
			foreach (var item in results)
			{
				Console.WriteLine(item.RestoreName);
			}

			Console.ReadLine();
		}
	}
}
