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
			Project p = Project.Db().Find(1);
			var changes = Project.Db().QueryChangeHistory(p.Id, -5).Take(5);

			foreach (var c in changes)
			{
				Console.WriteLine($"{c.DateTime} - version {c.Version}:");
				foreach (var prop in c.Properties)
				{
					Console.WriteLine($"\t{prop.PropertyName}: {prop.OldValue} -> {prop.NewValue}");
				}
				Console.WriteLine();
			}

			Console.ReadLine();
		}
	}
}
