namespace Postulate
{
	public class ValueComparison
	{
		public string PropertyName { get; set; }
		public object OldValue { get; set; }
		public object NewValue { get; set; }

		public bool IsChanged()
		{
			try
			{
				if (OldValue == null && NewValue == null) return false;
				if (OldValue == null ^ OldValue == null) return true;
				return !OldValue.Equals(NewValue);
			}
			catch
			{
				return true;
			}
		}
	}
}
