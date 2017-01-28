using Postulate.Attributes;
using Postulate.Extensions;
using Postulate.Merge;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Abstract
{
	public abstract class SqlGeneratorBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{		
		private bool _squareBraces = false;
		private string _defaultSchema = string.Empty;

		public SqlGeneratorBase(bool squareBraces = false, string defaultSchema = "")
		{
			_squareBraces = squareBraces;
			_defaultSchema = defaultSchema;
		}

		public string TableName(Type tableType = null)
		{
			Type t = (tableType == null) ? typeof(TRecord) : tableType;

			string result = DbObject.FromType(t).ToString();

			//if (_squareBraces) result = string.Join(".", result.Split('.').Select(s => $"[{s}]"));

			return result;
		}

		private string TableConstraintName()
		{
			Type t = typeof(TRecord);
			string result = t.Name;

			var attr = t.GetCustomAttribute<TableAttribute>();
			if (attr != null)
			{
				result = (!string.IsNullOrEmpty(attr.Schema) ? attr.Schema : string.Empty) + attr.Name;
			}

			return result;
		}

		private string[] GetWriteableColumns(Access option)
		{
			return GetWriteableProperties(option).Select(p => p.Name).ToArray();			
		}

		private IEnumerable<PropertyInfo> GetWriteableProperties(Access option)
		{
			Type t = typeof(TRecord);
			return t.GetProperties().Where(p => p.AllowAccess(option) && !p.HasAttribute<NotMappedAttribute>());
		}

		public string[] SelectableColumns(bool allColumns, bool squareBraces = false)
		{
			Type t = typeof(TRecord);

			IdentityColumnAttribute idAttr;
			string identityCol = (t.HasAttribute(out idAttr)) ? idAttr.ColumnName : SqlDb.IdentityColumnName;
			bool useAltIdentity = (!identityCol.Equals(SqlDb.IdentityColumnName));

			var props = t.GetProperties().Where(p => 
				p.CanRead && 
				CreateTable.IsSupportedType(p.PropertyType) &&
				!IsSupressedIdentity(useAltIdentity, p.SqlColumnName()));

			var results = (allColumns) ?
				props.Select(p => p.SqlColumnName()) :
				props.Where(p => !p.HasAttribute<LargeValueColumn>()).Select(p => p.SqlColumnName());

			if (squareBraces) results = results.Select(p => $"[{p}]");

			return results.ToArray();
		}

		private bool IsSupressedIdentity(bool useAltIdentity, string columnName)
		{
			if (useAltIdentity && columnName.Equals(SqlDb.IdentityColumnName)) return true;
			return false;
		}

		public string[] InsertColumns()
		{
			var result = GetWriteableColumns(Access.InsertOnly);
			if (_squareBraces) return result.Select(s => $"[{s}]").ToArray();
			return result;
		}

		public string[] InsertExpressions()
		{			
			return GetWriteableProperties(Access.InsertOnly).Select(pi =>
			{
				var expr = pi.GetCustomAttribute<InsertExpressionAttribute>() as InsertExpressionAttribute;
				if (expr != null) return expr.Expression;
				return $"@{pi.Name}";
			}).ToArray();
		}

		public string[] UpdateExpressions()
		{
			return GetWriteableProperties(Access.UpdateOnly).Select(pi =>
			{
				string result = (_squareBraces) ? $"[{pi.SqlColumnName()}]" : pi.SqlColumnName();
				var expr = pi.GetCustomAttributes(typeof(UpdateExpressionAttribute)).FirstOrDefault() as UpdateExpressionAttribute;
				if (expr != null) return result += $"={expr.Expression}";
				return result += $"=@{pi.Name}";
			}).ToArray();			
		}

		public abstract string FindStatement();

		public abstract string SelectStatement(bool allColumns = true);

		public abstract string UpdateStatement();

		public abstract string InsertStatement();

		public abstract string DeleteStatement();		
	}
}
