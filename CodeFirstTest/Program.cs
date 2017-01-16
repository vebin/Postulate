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
			PostulateDb db = new PostulateDb();
			using (SqlConnection cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				SchemaMerge merge = new SchemaMerge(typeof(PostulateDb), cn);
				foreach (var a in merge.Actions)
				{
					Console.WriteLine(a.ToString());
				}

				Console.WriteLine();
				//Console.WriteLine("Executing...");
				//merge.Execute(cn);
			}
			Console.ReadLine();
		}
	}
}
