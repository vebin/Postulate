using Postulate.Validation;
using System;
using System.Data;
using System.Reflection;

namespace Postulate.Validation
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class RequiredAttribute : ValidationAttribute
	{
		private bool _formatMessage = false;

		public RequiredAttribute() : base("Field '{0}' is required.")
		{
			_formatMessage = true;
		}

		public RequiredAttribute(string message) : base(message)
		{
		}

		public override bool IsValid(PropertyInfo property, object value, IDbConnection connection = null)
		{
			if (_formatMessage)
			{
				_message = string.Format(_message, property.Name);
				_formatMessage = false;
			}

			Type t = property.PropertyType;
			if (t.Equals(typeof(string)))
			{
				if (string.IsNullOrWhiteSpace(value?.ToString())) return false;
			}
			else
			{
				if (t.IsValueType && value != null)
				{
					// value types at their default values (i.e. int = 0) are considered invalid
					if (value.Equals(Activator.CreateInstance(t))) return false;
				}

				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					// nullable types with null value are considered invalid
					if (value == null) return false;					
				}
			}

			return true;
		}
	}
}
