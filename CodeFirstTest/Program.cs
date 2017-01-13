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
			LogEntry le = new LogEntry() { DateTime = DateTime.Now, Description = "hello from visual studio" };
			LogEntry.Db.Save(le);

			Console.WriteLine($"ID = {le.ID}");

			/*using (var cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				SchemaMerge merge = new SchemaMerge("CodeFirstTest.Models", cn);
				

				//Console.WriteLine(merge.ToString());
				//merge.SaveAs(@"c:\users\adam\desktop\Postulate.sql");
				merge.Execute(cn);
			}*/
			Console.ReadLine();
		}
	}
}
