using Dapper;
using System;

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
