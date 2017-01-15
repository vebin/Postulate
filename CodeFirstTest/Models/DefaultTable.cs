using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using Dapper;

namespace CodeFirstTest.Models
{
	[ColumnAccess("OrganizationID", Access.InsertOnly)]
	[ForeignKey("OrganizationID", typeof(Organization))]	
	[IdentityPosition(Position.EndOfTable)]
	public abstract class DefaultTable : DataRecord<int>
	{		
		[ColumnAccess(Access.InsertOnly)]
		[InsertExpression("dbo.LocalDateTime(@userName)")]	
		public DateTime DateCreated { get; set; }

		[MaxLength(20)]		
		[ColumnAccess(Access.InsertOnly)]
		[InsertExpression("@userName")]		
		public string CreatedBy { get; set; }		

		[ColumnAccess(Access.UpdateOnly)]
		[UpdateExpression("dbo.LocalDateTime(@userName)")]
		public DateTime? DateModified { get; set; }

		[ColumnAccess(Access.UpdateOnly)]
		[UpdateExpression("@userName")]
		[MaxLength(20)]
		public string ModifiedBy { get; set; }
	}
}
