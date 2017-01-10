using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Postulate.Validation;

namespace Postulate.Abstract
{
	public abstract class RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		public int RecordsPerPage { get; set; } = 50;

		public abstract TRecord Find(IDbConnection connection, TKey id);
		public abstract TRecord FindWhere(IDbConnection connection, string criteria, object parameters);
		public abstract IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, int page = 0);

		protected abstract TKey InsertExecute(IDbConnection connection, TRecord record);
		protected abstract void UpdateExecute(IDbConnection connection, TRecord record);

		public TKey Insert(IDbConnection connection, TRecord record)
		{
			Validate(record, connection);
			return InsertExecute(connection, record);
		}		

		public void Update(IDbConnection connection, TRecord record)
		{
			Validate(record, connection);
			UpdateExecute(connection, record);
		}

		public abstract void Delete(IDbConnection connection, TRecord record);

		public string DefaultQuery { get; set; }
		public string FindCommand { get; set; }		
		public string InsertCommand { get; set; }
		public string UpdateCommand { get; set; }
		public string DeleteCommand { get; set; }
		
		/// <summary>
		/// Throws a ValidationException if record fails any of its validation rules
		/// </summary>
		public void Validate(TRecord record, IDbConnection connection = null)
		{
			var errors = GetValidationErrors(record);
			if (errors.Any())
			{
				string message = "The record has one or more validation errors:\r\n";
				message += string.Join("\r\n", errors.Select(err => err));
				throw new ValidationException(message);
			}
		}

		public bool IsValid(TRecord record, IDbConnection connection = null)
		{
			return !GetValidationErrors(record, connection).Any();
		}

		public IEnumerable<string> GetValidationErrors(TRecord record, IDbConnection connection = null)
		{			
			foreach (var prop in record.GetType().GetProperties())
			{
				if (prop.PropertyType.Equals(typeof(DateTime)))
				{
					DateTime value = (DateTime)prop.GetValue(record);
					if (value.Equals(DateTime.MinValue)) yield return $"The {prop.Name} date field requires a value.";
				}

				var postulateAttr = prop.GetCustomAttributes<Validation.ValidationAttribute>();
				foreach (var attr in postulateAttr)
				{
					object value = prop.GetValue(record);
					if (!attr.IsValid(prop, value, connection)) yield return attr.ErrorMessage;					
				}

				var validationAttr = prop.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>();
				foreach (var attr in validationAttr)
				{
					object value = prop.GetValue(record);
					if (!attr.IsValid(value)) yield return attr.FormatErrorMessage(prop.Name);
				}
			}

			IValidateable validateable = record as IValidateable;
			if (validateable != null)
			{
				var errors = validateable.Validate(connection);
				foreach (var err in errors) yield return err;
			}
		}

		public void Save(IDbConnection connection, TRecord record)
		{
			SaveAction action;
			Save(connection, record, out action);
		}

		public void Save(IDbConnection connection, TRecord record, out SaveAction action)
		{
			if (record.IsNewRecord())
			{
				action = SaveAction.Insert;
				record.ID = Insert(connection, record);
			}
			else
			{
				action = SaveAction.Update;
				Update(connection, record);
			}
		}

		public bool TrySave(IDbConnection connection, TRecord record, out SaveAction action, out Exception exception)
		{
			exception = null;
			try
			{
				Save(connection, record, out action);
				return true;
			}
			catch (Exception exc)
			{
				action = SaveAction.NotSet;
				exception = exc;
				return false;
			}
		}

		public bool TrySave(IDbConnection connection, TRecord record, out Exception exception)
		{
			SaveAction action;
			return TrySave(connection, record, out action, out exception);
		}

		public abstract void Update(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns);
		
		public bool TryUpdate(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] expressions, out Exception setColumns)
		{
			setColumns = null;
			try
			{
				Update(connection, record, expressions);
				return true;
			}
			catch (Exception exc)
			{
				setColumns = exc;
				return false;				
			}
		}
	}
}
