using CodeFirstTest.Models;
using Dapper;
using Postulate;
using Postulate.Merge;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest
{
	class Program
	{
		static void Main(string[] args)
		{
			PostulateDb db = new PostulateDb();
			using (var cn = db.GetConnection() as SqlConnection)
			{
				cn.Open();
				SchemaMerge merge = new SchemaMerge("CodeFirstTest.Models", cn);
				Console.WriteLine(merge.ToString());
			}
			Console.ReadLine();
		}
	}
}
