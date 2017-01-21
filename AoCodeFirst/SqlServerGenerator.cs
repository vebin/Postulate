using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Postulate.Extensions;

namespace Postulate
{
	internal class SqlServerGenerator<TRecord, TKey> : SqlGeneratorBase<TRecord, TKey> where TRecord : DataRecord<TKey>
	{
		public SqlServerGenerator() : base(squareBraces:true, defaultSchema:"dbo")
		{
		}

		public override string FindStatement()
		{
			return $"SELECT {string.Join(", ", SelectableColumns(true, true))} FROM {TableName()} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
		}

		public override string DeleteStatement()
		{
			return $"DELETE {TableName()} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
		}

		public override string InsertStatement()
		{
			return $@"INSERT INTO {TableName()} (
				{string.Join(", ", InsertColumns())}
			) OUTPUT [inserted].[ID] VALUES (
				{string.Join(", ", InsertExpressions())}
			)";
		}

		public override string UpdateStatement()
		{
			return $"UPDATE {TableName()} SET {string.Join(", ", UpdateExpressions())} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
		}

		public override string SelectStatement(bool allColumns = true)
		{
			return $"SELECT {string.Join(", ", SelectableColumns(allColumns, true))} FROM {TableName()}";
		}
	}
}