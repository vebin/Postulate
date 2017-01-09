using Postulate.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace CodeFirstTest.Models
{
	[UniqueKey(nameof(OrganizationID), nameof(UpcCode))]
	public class Item : DefaultTable
	{
		[PrimaryKey]
		public int OrganizationID { get; set; }
		[PrimaryKey]
		public string Name { get; set; }
		
		public string UpcCode { get; set; }

		[MaxLength(255)]
		public string Description { get; set; }

		[Postulate.Validation.RequiredAttribute]
		public decimal UnitCost { get; set; }
		[Postulate.Validation.RequiredAttribute]
		public decimal UnitPrice { get; set; }
		public decimal QtyOnHand { get; set; }
		public decimal ReorderQty { get; set; }

		[Postulate.Validation.RequiredAttribute]
		public DateTime EffectiveDate { get; set; }
	}
}
