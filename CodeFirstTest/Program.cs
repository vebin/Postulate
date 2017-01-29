using CodeFirstTest.Models;
using Ginseng.Models;
using Postulate.Merge;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace CodeFirstTest
{
	class Program
	{
		static void Main(string[] args)
		{
			GinsengDb db = new GinsengDb();

			SchemaMerge sm = new SchemaMerge(typeof(GinsengDb));
			using (SqlConnection cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				var changes = sm.Analyze(cn);
			}

			Console.ReadLine();
		}
	}
}
