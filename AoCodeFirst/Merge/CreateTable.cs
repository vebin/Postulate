using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
	public class CreateTable : SchemaMerge.Action
	{
		private readonly Type _modelType;

		public CreateTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Create)
		{
			_modelType = modelType;
		}

		public override string SqlScript()
		{
			throw new NotImplementedException();
		}
	}
}
