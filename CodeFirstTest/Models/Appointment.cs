using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Attributes;

namespace CodeFirstTest.Models
{
	public class Appointment : DefaultTable
	{
		public int OrganizationID { get; set; }

		//[Postulate.Attributes.ForeignKey(typeof(Customer))]
		//public int CustomerId { get; set; }

		[Column(TypeName = "date")]
		public DateTime Date { get; set; }		
		public TimeSpan StartTime { get; set; }		
		public TimeSpan EndTime { get; set; }
		[MaxLength(255)]
		public string Comments { get; set; }
	}
}
