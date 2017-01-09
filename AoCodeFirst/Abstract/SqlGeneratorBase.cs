using Postulate.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Abstract
{
	public abstract class SqlGeneratorBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{		
		protected const string _idColumn = "ID";

		private bool _squareBraces = false;
		private string _defaultSchema = string.Empty;

		public SqlGeneratorBase(bool squareBraces = false, string defaultSchema = "")
		{
			_squareBraces = squareBraces;
			_defaultSchema = defaultSchema;
		}

		public string TableName()
		{
			Type t = typeof(TRecord);
			string result = t.Name;

			TableAttribute attr = t.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
			if (attr != null)
			{
				if (!string.IsNullOrEmpty(attr.Schema)) result = $"{attr.Schema}.";
				result += attr.Name;
			}
			else
			{
				if (!string.IsNullOrEmpty(_defaultSchema))
				{
					result = $"{_defaultSchema}.{t.Name}";
				}
			}

			if (_squareBraces) result = string.Join(".", result.Split('.').Select(s => $"[{s}]"));

			return result;
		}		

		private string[] GetWriteableColumns(AccessOption option)
		{
			return GetWriteableProperties(option).Select(p => p.Name).ToArray();			
		}

		private IEnumerable<PropertyInfo> GetWriteableProperties(AccessOption option)
		{
			Type t = typeof(TRecord);
			return t.GetProperties().Where(p => AllowAccess(p, option));
		}

		public string[] SelectableColumns(bool allColumns, bool squareBraces = false)
		{
			Type t = typeof(TRecord);
			var props = t.GetProperties().Where(p => p.CanRead);
			var results = (allColumns) ?
				props.Select(p => ColumnName(p)) :
				props.Where(p => p.GetCustomAttribute<LargeValueColumn>() == null).Select(p => ColumnName(p));

			if (squareBraces) results = results.Select(p => $"[{p}]");

			return results.ToArray();
		}

		public string[] InsertColumns()
		{
			var result = GetWriteableColumns(AccessOption.InsertOnly);
			if (_squareBraces) return result.Select(s => $"[{s}]").ToArray();
			return result;
		}

		public string[] InsertExpressions()
		{			
			return GetWriteableProperties(AccessOption.InsertOnly).Select(pi =>
			{
				var expr = pi.GetCustomAttribute<InsertExpressionAttribute>() as InsertExpressionAttribute;
				if (expr != null) return expr.Expression;
				return $"@{ColumnName(pi)}";
			}).ToArray();
		}

		public string[] UpdateExpressions()
		{
			return GetWriteableProperties(AccessOption.UpdateOnly).Select(pi =>
			{
				string result = (_squareBraces) ? $"[{ColumnName(pi)}]" : ColumnName(pi);
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
		
		private static bool AllowAccess(PropertyInfo pi, AccessOption option)
		{
			if (pi.CanWrite && !pi.Name.Equals(_idColumn) && !pi.GetCustomAttributes<CalculatedAttribute>().Any())
			{
				ColumnAccessAttribute attr = null;
				attr = pi.DeclaringType.GetCustomAttribute<ColumnAccessAttribute>() as ColumnAccessAttribute;
				if (attr != null && pi.Name.Equals(attr.ColumnName)) return attr.Access == option;

				attr = pi.GetCustomAttribute<ColumnAccessAttribute>() as ColumnAccessAttribute;
				if (attr != null) return attr.Access == option;

				return true;
			}
			return false;
		}

		private static string ColumnName(PropertyInfo pi)
		{
			string result = pi.Name;

			var attr = pi.GetCustomAttribute<ColumnAttribute>() as ColumnAttribute;
			if (attr != null) result = attr.Name;

			return result;
		}
	}
}
