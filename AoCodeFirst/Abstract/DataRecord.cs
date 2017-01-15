using System;

namespace Postulate.Abstract
{
	public abstract class DataRecord<TKey>
	{
		private TKey _id;
		public TKey Id
		{
			get { return _id; }
			set { if (IsNewRecord()) { _id = value; } else { throw new InvalidOperationException("Can't set the ID property more than once."); } }
		}

		public bool IsNewRecord()
		{
			return (Id.Equals(default(TKey)));
		}
	}
}
