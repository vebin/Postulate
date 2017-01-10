﻿using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Postulate.Validation;
using Postulate.Attributes;
using Dapper;

namespace Postulate.Abstract
{
	public abstract class RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		public int RecordsPerPage { get; set; } = 50;

		public abstract bool TableExists(IDbConnection connection);
		public abstract TRecord Find(IDbConnection connection, TKey id);
		public abstract TRecord FindWhere(IDbConnection connection, string criteria, object parameters);
		public abstract IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, int page = 0);

		protected abstract TKey OnInsert(IDbConnection connection, TRecord record, object parameters = null);
		protected abstract void OnUpdate(IDbConnection connection, TRecord record, object parameters = null);

		public TKey Insert(IDbConnection connection, TRecord record, object parameters = null)
		{
			Validate(record, connection);
			return OnInsert(connection, record, parameters);
		}		

		public void Update(IDbConnection connection, TRecord record, object parameters = null)
		{
			Validate(record, connection);
			OnUpdate(connection, record, parameters);
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
				if (RequiredDateNotSet(prop, record))
				{					
					yield return $"The {prop.Name} date field requires a value.";
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

		public void Save(IDbConnection connection, TRecord record, object parameters = null)
		{
			SaveAction action;
			Save(connection, record, out action, parameters);
		}

		public void Save(IDbConnection connection, TRecord record, out SaveAction action, object parameters = null)
		{
			if (record.IsNewRecord())
			{
				action = SaveAction.Insert;
				record.ID = Insert(connection, record, parameters);
			}
			else
			{
				action = SaveAction.Update;
				Update(connection, record, parameters);
			}
		}

		public bool TrySave(IDbConnection connection, TRecord record, out SaveAction action, out Exception exception, object parameters = null)
		{
			exception = null;
			try
			{
				Save(connection, record, out action, parameters);
				return true;
			}
			catch (Exception exc)
			{
				action = SaveAction.NotSet;
				exception = exc;
				return false;
			}
		}

		public bool TrySave(IDbConnection connection, TRecord record, out Exception exception, object parameters = null)
		{
			SaveAction action;
			return TrySave(connection, record, out action, out exception, parameters);
		}

		public abstract void Update(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns, object parameters = null);
		
		public bool TryUpdate(IDbConnection connection, TRecord record, Expression<Func<TRecord, object>>[] setColumns, out Exception exception, object parameters = null)
		{
			exception = null;
			try
			{
				Update(connection, record, setColumns, parameters);
				return true;
			}
			catch (Exception exc)
			{
				exception = exc;
				return false;				
			}
		}

		private bool RequiredDateNotSet(PropertyInfo prop, TRecord record)
		{
			if (prop.PropertyType.Equals(typeof(DateTime)))
			{
				DateTime value = (DateTime)prop.GetValue(record);
				if (value.Equals(DateTime.MinValue))
				{
					if (record.IsNewRecord() && prop.GetCustomAttribute<InsertExpressionAttribute>() == null) return true;
					if (!record.IsNewRecord() && prop.GetCustomAttribute<UpdateExpressionAttribute>() == null) return true;
				}
			}
			return false;
		}
	}
}
