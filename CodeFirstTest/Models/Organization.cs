using Postulate;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Postulate.Attributes;

namespace CodeFirstTest.Models
{	
	public class Organization : DefaultTable
	{
		[MaxLength(100)]
		[Required]
		[PrimaryKey]		
		public string Name { get; set; }		

		[MaxLength(255)]
		public string Description { get; set; }

		public static SqlServerRowManager<Organization, int> Db()
		{
			return new SqlServerRowManager<Organization, int>(new PostulateDb());
		}
	}
}
