using System.Data.SqlClient;
using System.Diagnostics;
using Dapper;
using System.Data;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using Postulate.Extensions;

namespace Postulate
{
	public class Profiler
    {
		private Stopwatch _sw = null;
		private CommandDefinition _cmdDef;
		private string _source;

		private const string logSchema = "log";
		private const string logTable = "SqlDbProfiler";		

		public Profiler()
		{
		}

		public virtual bool ShouldLog(IDbConnection connection, CommandDefinition commandDef, string source)
		{
			return false;
		}

		public virtual bool ShouldInitialize()
		{
			return true;
		}

		public string Source { get; set; }

		/// <summary>
		/// Creates the table log.SqlDbProfiler for capturing query run times
		/// </summary>
		/// <param name="connection"></param>
        public void Initalize(IDbConnection connection)
        {
			if (!ShouldInitialize()) return;

            if (!connection.Exists($"SELECT 1 FROM [sys].[schemas] WHERE [name]='{logSchema}'", null))
            {
                connection.Execute($"CREATE SCHEMA [{logSchema}]", null, commandType: CommandType.Text);
			}
            
            if (!connection.Exists($"SELECT * FROM sys.tables WHERE [schema_id]=SCHEMA_ID('{logSchema}') AND [name]='{logTable}'", null))
            {
                connection.Execute(
                    $@"CREATE TABLE [{logSchema}].[{logTable}] (
						[DateTime] datetime NOT NULL DEFAULT (getutcdate()),
                        [Command] varchar(max) NOT NULL,
						[Type] int NOT NULL,
                        [Parameters] varchar(max) NULL,
                        [Milleseconds] bigint NOT NULL,
						[Source] varchar(255) NULL,
                        [ID] int identity(1,1) PRIMARY KEY
                    )", null, commandType: CommandType.Text);
            }
        }		
		
		internal void Start(IDbConnection connection, CommandDefinition cmdDef, [CallerMemberName]string source = null)
		{
			if (!ShouldLog(connection, cmdDef, source)) return;

			Initalize(connection);
			_cmdDef = cmdDef;
			_source = source;		

			if (!string.IsNullOrEmpty(Source)) _source = Source;

			_sw = Stopwatch.StartNew();
		}

		internal void Stop(IDbConnection connection)
		{
			if (_sw == null || !_sw.IsRunning) return;

			_sw.Stop();
			connection.Execute(
				$@"INSERT INTO [{logSchema}].[{logTable}] (
					[Command], [Type], [Parameters], [Milleseconds], [Source]
				) VALUES (
					@cmdText, @type, @paramValues, @ms, @source
				)", 
				new
				{
					cmdText = _cmdDef.CommandText,
					type = _cmdDef.CommandType ?? CommandType.Text,
					paramValues = ParamInfo(_cmdDef.Parameters),
					ms = _sw.ElapsedMilliseconds,
					source = _source
				});
		}

		private string ParamInfo(object parameters)
		{
			if (parameters == null) return null;

			SqlParameter[] paramArray = parameters as SqlParameter[];
			if (paramArray != null)
			{
				return string.Join(", ", paramArray.Select(p => $"{p.ParameterName} = {p.Value.ToString()}"));
			}

			DynamicParameters dp = parameters as DynamicParameters;
			if (dp != null)
			{
				return string.Join(", ", dp.ParameterNames.Select(p =>
				{
					var value = dp.Get<dynamic>(p);
					string showValue = value.ToString();
					showValue = IntArrayToString(value);
					return $"{p} = {showValue}";
				}));
			}

			PropertyInfo[] props = parameters.GetType().GetProperties();
			return string.Join(", ", props.Select(p => 
			{
				object showValue = p.GetValue(parameters);
				showValue = IntArrayToString(showValue);
				return $"{p.Name} = {(showValue ?? "<null>").ToString()}";
			}));
		}

		private static string IntArrayToString(object value)
		{
			if (value == null) return "<null>";
			int[] intValues = value as int[];
			if (intValues != null) return "[" + string.Join(", ", intValues.Select(i => i.ToString())) + "]";
			return value.ToString();
		}
	}
}
