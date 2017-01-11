using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	public class CreateTable : SchemaMerge.Action
	{
		public CreateTable() : base(MergeObjectType.Table, MergeActionType.Create)
		{
		}

		public override string SqlScript()
		{
			throw new NotImplementedException();
		}
	}
}
