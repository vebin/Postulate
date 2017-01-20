using Postulate.Enums;
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
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Extensions;

namespace Postulate.Abstract
{
	public delegate void SavingRecordHandler<TRecord>(IDbConnection connection, SaveAction action, TRecord record);
	public delegate void RecordSavedHandler<TRecord>(IDbConnection connection, SaveAction action, TRecord record);

	public abstract class RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		public int RecordsPerPage { get; set; } = 50;
		
		public abstract TRecord Find(IDbConnection connection, TKey id);
		public abstract TRecord FindWhere(IDbConnection connection, string criteria, object parameters);
		public abstract IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, int page = 0);

		protected abstract TKey OnInsert(IDbConnection connection, TRecord record, object parameters = null);
		protected abstract void OnUpdate(IDbConnection connection, TRecord record, object parameters = null);

		public TKey Insert(IDbConnection connection, TRecord record, object parameters = null)
		{
			ThrowUnmapped();
			Validate(record, connection);
			return OnInsert(connection, record, parameters);
		}

		public void Update(IDbConnection connection, TRecord record, object parameters = null)
		{
			ThrowUnmapped();
			Validate(record, connection);
			OnUpdate(connection, record, parameters);
		}

		private void ThrowUnmapped()
		{
			if (!IsMapped()) throw new InvalidOperationException($"The model class {typeof(TRecord).Name} is marked as [NotMapped].");
		}

		public abstract void Delete(IDbConnection connection, TRecord record, object parameters = null);

		public bool TryDelete(IDbConnection connection, TRecord record, out Exception exception, object parameters = null)
		{
			try
			{
				exception = null;
				Delete(connection, record, parameters);
				return true;
			}
			catch (Exception exc)
			{
				exception = exc;
				return false;
			}
		}

		public bool TryFind(IDbConnection connection, TKey id, out TRecord record)
		{
			record = Find(connection, id);
			return (record != null);
		}

		public bool TryFindWhere(IDbConnection connection, string criteria, object parameters, out TRecord record)
		{
			record = FindWhere(connection, criteria, parameters);
			return (record != null);
		}

		public SavingRecordHandler<TRecord> SavingRecord { get; set; }
		public RecordSavedHandler<TRecord> RecordSaved { get; set; }

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
					if (!IsRequiredWithInsertExpression(prop, attr))
					{
						object value = prop.GetValue(record);
						if (!attr.IsValid(value)) yield return attr.FormatErrorMessage(prop.Name);
					}
				}
			}

			IValidateable validateable = record as IValidateable;
			if (validateable != null)
			{
				var errors = validateable.Validate(connection);
				foreach (var err in errors) yield return err;
			}
		}

		private bool IsRequiredWithInsertExpression(PropertyInfo prop, System.ComponentModel.DataAnnotations.ValidationAttribute attr)
		{
			// required properties with an insert expression should not be validated as a regular required field
			return prop.HasAttribute<InsertExpressionAttribute>() && attr.GetType().Equals(typeof(RequiredAttribute)));
		}

		public void Save(IDbConnection connection, TRecord record, object parameters = null)
		{
			SaveAction action;
			Save(connection, record, out action, parameters);
		}

		public void Save(IDbConnection connection, TRecord record, out SaveAction action, object parameters = null)
		{
			action = (record.IsNewRecord()) ? SaveAction.Insert : SaveAction.Update;
			SavingRecord?.Invoke(connection, action, record);

			if (record.IsNewRecord())
			{			
				record.Id = Insert(connection, record, parameters);
			}
			else
			{				
				Update(connection, record, parameters);
			}

			RecordSaved?.Invoke(connection, action, record);
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

		public abstract void Update(IDbConnection connection, TRecord record, object parameters, params Expression<Func<TRecord, object>>[] setColumns);
		
		public bool TryUpdate(IDbConnection connection, TRecord record, object parameters, out Exception exception, params Expression<Func<TRecord, object>>[] setColumns)
		{
			exception = null;
			try
			{
				Update(connection, record, parameters, setColumns);
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

		protected string PropertyNameFromLambda(Expression expression)
		{
			// thanks to http://odetocode.com/blogs/scott/archive/2012/11/26/why-all-the-lambdas.aspx
			// thanks to http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

			LambdaExpression le = expression as LambdaExpression;
			if (le == null) throw new ArgumentException("expression");

			MemberExpression me = null;
			if (le.Body.NodeType == ExpressionType.Convert)
			{
				me = ((UnaryExpression)le.Body).Operand as MemberExpression;
			}
			else if (le.Body.NodeType == ExpressionType.MemberAccess)
			{
				me = le.Body as MemberExpression;
			}

			if (me == null) throw new ArgumentException("expression");

			return me.Member.Name;
		}

		protected bool IsMapped()
		{
			return !typeof(TRecord).HasAttribute<NotMappedAttribute>();
		}
	}
}
