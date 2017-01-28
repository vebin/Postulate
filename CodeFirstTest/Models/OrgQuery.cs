using Postulate;
using Postulate.Abstract;
using Postulate.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	public class OrgQuery : SqlServerQuery<Organization>
	{
		public OrgQuery() : base("SELECT * FROM [dbo].[Organization]", new PostulateDb())
		{
		}

		[QueryField("[Name] LIKE '%'+@name+'%'")]
		public string Name { get; set; }
	}
}
