using Postulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	public class PostulateDb : SqlServerDb
	{		
		public PostulateDb() : base("PostulateTest")
		{
		}
	}
}
