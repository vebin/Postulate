using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Abstract
{	
	public abstract class DataRecord<TKey>
	{
		private TKey _id;
		public TKey ID
		{
			get { return _id; }
			set { if (_id.Equals(default(TKey))) { _id = value; } else { throw new InvalidOperationException("Can't set the ID property more than once."); } }
		}

		public bool IsNewRecord()
		{
			return (ID.Equals(default(TKey)));
		}
	}
}
