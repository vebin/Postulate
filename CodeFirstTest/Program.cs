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
			db.MergeSchema();

			Console.ReadLine();
		}
	}
}
