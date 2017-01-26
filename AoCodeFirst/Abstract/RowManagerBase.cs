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
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Extensions;
using Postulate.Models;

namespace Postulate.Abstract
{
	public delegate void SavingRecordHandler<TRecord>(IDbConnection connection, SaveAction action, TRecord record);
	public delegate void RecordSavedHandler<TRecord>(IDbConnection connection, SaveAction action, TRecord record);
	public delegate bool CheckPermissionHandler<TRecord>(IDbConnection connection, Permission permission, TRecord record);

	public abstract class RowManagerBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		public int RecordsPerPage { get; set; } = 50;

		public TRecord Find(IDbConnection connection, TKey id)
		{
			var record = OnFind(connection, id);
			CheckFindPermission(connection, record);
			return record;
		}

		protected abstract TRecord OnFind(IDbConnection connection, TKey id);

		public TRecord FindWhere(IDbConnection connection, string criteria, object parameters)
		{
			var record = OnFindWhere(connection, criteria, parameters);
			CheckFindPermission(connection, record);
			return record;
		}

		private void CheckFindPermission(IDbConnection connection, TRecord record)
		{
			if (record == null) return;

			if (!CheckPermission?.Invoke(connection, Permission.Read, record) ?? true)
			{
				throw new UnauthorizedAccessException($"Read permission was denied on the {typeof(TRecord).Name} with Id {record.Id}.");
			}
		}

		protected abstract TRecord OnFindWhere(IDbConnection connection, string criteria, object parameters);

		public abstract IEnumerable<TRecord> Query(IDbConnection connection, string criteria, object parameters, string orderBy, int page = 0);

		protected abstract TKey OnInsert(IDbConnection connection, TRecord record, object parameters = null);
		protected abstract void OnUpdate(IDbConnection connection, TRecord record, object parameters = null);

		public TKey Insert(IDbConnection connection, TRecord record, object parameters = null)
		{
			ThrowUnmapped();
			Validate(record, SaveAction.Insert, connection);
			return OnInsert(connection, record, parameters);
		}

		public void Update(IDbConnection connection, TRecord record, object parameters = null)
		{
			ThrowUnmapped();
			Validate(record, SaveAction.Update, connection);
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

		public CheckPermissionHandler<TRecord> CheckPermission { get; set; }
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
		public void Validate(TRecord record, SaveAction action = SaveAction.NotSet, IDbConnection connection = null)
		{
			var errors = GetValidationErrors(record, action);
			if (errors.Any())
			{
				string message = "The record has one or more validation errors:\r\n";
				message += string.Join("\r\n", errors.Select(err => err));
				throw new ValidationException(message);
			}
		}

		public bool IsValid(TRecord record, SaveAction action = SaveAction.NotSet, IDbConnection connection = null)
		{
			return !GetValidationErrors(record, action, connection).Any();
		}

		public IEnumerable<string> GetValidationErrors(TRecord record, SaveAction action = SaveAction.NotSet, IDbConnection connection = null)
		{			
			foreach (var prop in record.GetType().GetProperties().Where(pi => pi.HasSaveAction(action)))
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
			return prop.HasAttribute<InsertExpressionAttribute>() && attr.GetType().Equals(typeof(RequiredAttribute));
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
				string ignoreProps;
				if (HasChangeTracking(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);
				Update(connection, record, parameters);
			}

			RecordSaved?.Invoke(connection, action, record);
		}

		private bool HasChangeTracking(out string ignoreProperties)
		{
			TrackChangesAttribute attr;
			if (typeof(TRecord).HasAttribute(out attr))
			{
				ignoreProperties = attr.IgnoreProperties;
				return true;
			}
			ignoreProperties = null;
			return false;
			
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

		protected abstract void OnUpdate(IDbConnection connection, TRecord record, object parameters, params Expression<Func<TRecord, object>>[] setColumns);

		public void Update(IDbConnection connection, TRecord record, object parameters, params Expression<Func<TRecord, object>>[] setColumns)
		{
			string ignoreProps;
			if (HasChangeTracking(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);
			OnUpdate(connection, record, parameters, setColumns);
		}
		
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

		public IEnumerable<PropertyChange> GetChanges(IDbConnection connection, TRecord record, string ignoreProps = null)
		{
			if (record.IsNewRecord()) return null;

			string[] ignorePropsArray = (ignoreProps ?? string.Empty).Split(',', ';').Select(s => s.Trim()).ToArray();

			TRecord savedRecord = Find(connection, record.Id);
			return typeof(TRecord).GetProperties().Where(pi => pi.AllowAccess(Access.UpdateOnly) && !ignorePropsArray.Contains(pi.Name)).Select(pi =>
			{
				return new PropertyChange()
				{
					PropertyName = pi.Name,
					OldValue = OnGetChangesPropertyValue(pi, savedRecord, connection),
					NewValue = OnGetChangesPropertyValue(pi, record, connection)
				};
			}).Where(vc => vc.IsChanged());
		}

		protected virtual object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
		{
			return propertyInfo.GetValue(record);
		}

		public void CaptureChanges(IDbConnection connection, TRecord record, string ignoreProps = null)
		{
			var changes = GetChanges(connection, record, ignoreProps);
			if (changes != null && changes.Any()) OnCaptureChanges(connection, record.Id, changes);			
		}

		public abstract IEnumerable<ChangeHistory<TKey>> QueryChangeHistory(IDbConnection connection, TKey id, int timeZoneOffset = 0);		

		protected abstract void OnCaptureChanges(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes);

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
