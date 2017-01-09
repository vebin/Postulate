using Postulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	public class NcmirDb : SqlDb
	{		
		public NcmirDb() : base("Ncmir")
		{
		}

		private SqlServerRowManager<Item, int> _item;
		public SqlServerRowManager<Item, int> Item
		{
			get
			{
				if (_item == null) _item = new SqlServerRowManager<Item, int>(ConnectionString);				
				return _item;
			}
		}

		private SqlServerRowManager<Customer, int> _customer;
		public SqlServerRowManager<Customer, int> Customer
		{
			get
			{
				if (_customer == null) _customer = new SqlServerRowManager<Customer, int>(ConnectionString);
				return _customer;
			}
		}
	}
}
