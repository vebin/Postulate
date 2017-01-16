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
			Organization org = Organization.Db().Find(1);
			org.Name = "This As Well";
			Organization.Db().Update(org, new { userName = "adamo" }, o => o.Name);
			
		}
	}
}
