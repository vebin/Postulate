using Postulate.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System;
using System.Data;

namespace CodeFirstTest.Models
{
	public class Customer : DefaultTable, IValidateable
	{
		public int OrganizationID { get; set; }

		[MaxLength(50)]
		[Required]
		public string LastName { get; set; }

		[MaxLength(50)]
		[Required]
		public string FirstName { get; set; }

		[MaxLength(50)]		
		public string Address { get; set; }

		[MaxLength(50)]
		public string City { get; set; }

		[MaxLength(20)]
		public string PostalCode { get; set; }

		[MaxLength(2)]
		public string State { get; set; }
		public string HomePhone { get; set; }
		public string WorkPhone { get; set; }
		public string MobilePhone { get; set; }
		[Regex(Patterns.Email, "Email address does not appear valid.")]
		public string Email { get; set; }
		
		public DateTime EffectiveDate { get; set; }

		public IEnumerable<string> Validate(IDbConnection connection = null)
		{
			var phoneFields = new string[] { HomePhone, WorkPhone, MobilePhone, Email };
			if (phoneFields.All(item => string.IsNullOrEmpty(item)))
			{
				yield return "Must provide at lease one phone number or email address.";
			}
		}
	}
}
