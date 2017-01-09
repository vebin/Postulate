using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstTest.Models
{
	public class Appointment : DefaultTable
	{
		public int OrganizationID { get; set; }

		[Column(TypeName = "date")]
		public DateTime Date { get; set; }		
		public TimeSpan StartTime { get; set; }		
		public TimeSpan EndTime { get; set; }
		[MaxLength(255)]
		public string Comments { get; set; }
	}
}
