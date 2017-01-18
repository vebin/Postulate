using CodeFirstTest.Models;
using Postulate.Merge;
using System;
using System.Data.SqlClient;

namespace CodeFirstTest
{
	class Program
	{
		static void Main(string[] args)
		{
			/*Organization org = Organization.Db().Find(1);
			org.BillingRate = 30;
			org.Description = "This better be a good description";
			Organization.Db().Update(org, new { userName = "adamo" }, o => o.BillingRate, o => o.Description);*/

			PostulateDb db = new PostulateDb();
			SchemaMerge merge = new SchemaMerge(typeof(PostulateDb));
			using (SqlConnection cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				
				foreach (var a in merge.Actions)
				{
					Console.WriteLine(a.ToString());
					foreach (var cmd in a.SqlCommands())
					{
						Console.WriteLine(cmd);
						Console.WriteLine();
					}
				}

				Console.WriteLine();
				Console.WriteLine("Executing...");
				merge.Execute(cn);
			}
			Console.ReadLine();
		}
	}
}
