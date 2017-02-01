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
			GinsengDb db = new GinsengDb();

			SchemaMerge sm = new SchemaMerge(typeof(GinsengDb));
			//IEnumerable<SchemaMerge.Action> changes = null;		

			using (SqlConnection cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				//changes = sm.Analyze(cn);
				sm.SaveAs(cn, @"c:\users\adam\Desktop\changes.sql");
			}

			foreach (var a in sm.AllActions)
			{
				Console.WriteLine(a.ToString());
				foreach (var cmd in sm.AllCommands[a])
				{
					Console.WriteLine(cmd);
					Console.WriteLine();
				}
			}

			Console.ReadLine();
		}
	}
}
