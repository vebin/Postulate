using CodeFirstTest.Models;
using Dapper;
using System;
using System.Collections.Generic;
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

			var c = db.Customer.Find(1);
			c.Email = "adamosoftware@gmail.com";
			db.Customer.Save(c, new DynamicParameters(new { userName = "adamo" }));
			return;

			/*Customer c = new Customer();
			c.FirstName = "Adam";
			c.LastName = "O'Neil";
			c.MobilePhone = "864-373-4637";
			c.EffectiveDate = DateTime.Today;

			db.Customer.Save(c, new DynamicParameters(new { userName = "adamo" }));

			Console.WriteLine($"customer id = {c.ID}");

			Console.ReadLine();*/
		}
	}
}
