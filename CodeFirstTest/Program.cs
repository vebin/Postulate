using CodeFirstTest.Models;
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
			NcmirDb db = new NcmirDb();
			
			Customer c = new Customer();
			c.FirstName = "Adam";
			c.MobilePhone = "864-373-4637";
			c.EffectiveDate = DateTime.Today;			

			var errors = db.Customer.GetValidationErrors(c);
			foreach (var err in errors)
			{
				Console.WriteLine(err);
			}
			
			Console.ReadLine();
		}
	}
}
