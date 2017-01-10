using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using Dapper;

namespace CodeFirstTest.Models
{
	[ColumnAccess("OrganizationID", AccessOption.InsertOnly)]
	[ForeignKey("OrganizationID", typeof(Organization))]	
	[IdentityPosition(Position.EndOfTable)]
	public abstract class DefaultTable : DataRecord<int>
	{		
		[ColumnAccess(AccessOption.InsertOnly)]
		[InsertExpression("dbo.LocalDateTime(@userName)")]	
		public DateTime DateCreated { get; set; }

		[MaxLength(20)]		
		[ColumnAccess(AccessOption.InsertOnly)]
		[InsertExpression("@userName")]		
		public string CreatedBy { get; set; }		

		[ColumnAccess(AccessOption.UpdateOnly)]
		[UpdateExpression("dbo.LocalDateTime(@userName)")]
		public DateTime? DateModified { get; set; }

		[ColumnAccess(AccessOption.UpdateOnly)]
		[UpdateExpression("@userName")]
		[MaxLength(20)]
		public string ModifiedBy { get; set; }
	}
}
