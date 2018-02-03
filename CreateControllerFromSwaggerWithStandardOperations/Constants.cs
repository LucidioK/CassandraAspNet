using System;
using System.Collections.Generic;
using System.Text;

namespace CreateControllerFromSwaggerWithStandardOperations
{
    class Constants
    {
        public static string AppSettingsCode = @"
namespace ^NamespaceBase^
{
    public class AppSettings
    {
 
    }
}
";
        public static string ConstantsCode = @"
namespace ^NamespaceBase^
{
	using System;
	using System.Text;
	
    public class Constants
    {	  
		public static string ConnectionString = ""^ConnectionString^"";

        public static int MaximumNumberOfRows = ^MaximumNumberOfRows^;

        public static string CamelCase(string name)
        {
            var cSharpified = CSharpifyName(name);
            return cSharpified.Substring(0, 1).ToLowerInvariant() + cSharpified.Substring(1);
        }

        public static string CSharpifyName(string name)
        {
            var cassandrified = new StringBuilder();
            var previousWasUnderline = false;
            foreach (var c in name)
            {
                if (c == '_')
                {
                    previousWasUnderline = true;
                    continue;
                }
                cassandrified.Append(
                    cassandrified.Length == 0 || previousWasUnderline ?
                        char.ToUpperInvariant(c) :
                        c);
                previousWasUnderline = false;
            }
            return cassandrified.ToString();
        }
    }
}
";
        public static string ControllerCode = @"
namespace ^NamespaceBase^.V^ApiVersion^.Controllers
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Extensions.Caching.Memory;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using ^NamespaceBase^.Entities;
	using Cassandra;
	using Cassandra.Data.Linq;
    using System.Linq;

    //[ApiVersion(""^ApiVersion^"")]
    [Produces(""application/json"")]
    [Route(""v^ApiVersion^/^EntityNameCamelCase^"")]
    public class ^EntityName^Controller : Controller
    {
        private readonly AppSettings _config;
        private readonly IMemoryCache _cache;
        private readonly ILogger<^EntityName^Controller> _logger;
        private Session _session;
        private string _keySpaceName;
		
        public ^EntityName^Controller(IOptions<AppSettings> appSettings, IMemoryCache cache, ILogger<^EntityName^Controller> logger)
        {
            _config = appSettings.Value;
            _cache = cache;
            _logger = logger;
			_session = (Session)Cluster.Builder().WithConnectionString(Constants.ConnectionString).Build().Connect();
            UdtMapping.Map(_session);
        }
		
^ProducesResponseAttributes^
        [HttpGet(""^EntityNameCamelCase^"")]
        public async Task<IActionResult> Get^EntityName^(^FilterParameters^)
        {
			return await Get(null);
		}

		
^ProducesResponseAttributes^
        [HttpGet(""^EntityNameCamelCase^/{id}"")]
        public async Task<IActionResult> Get^EntityName^ById(^PrimaryKeyColumnType^ id^FilterParametersWithPrecedingComa^)
        {
			return await Get(id);
		}
		
		private async Task<IActionResult> Get(^PrimaryKeyColumnType^? id^FilterParametersWithPrecedingComa^)
        {
			try
			{
				var table = new Table<^EntityName^>(_session);
				
				var rows = 
					id == null 
					? await table.ExecuteAsync()
					: await table
					  .Where(r => r.^PrimaryKeyColumnName^ == id)
					  .ExecuteAsync();
				
^OptionalRowFilteringCode^
				rows = rows.Take(Constants.MaximumNumberOfRows);
				return Ok(rows);
			}
			catch (Exception e)
			{
				_logger.LogError(e.Message);
				throw;
			}
		}
	}
}

";
        public static string ProducesResponseType = "		[ProducesResponseType(typeof(^EntityName^), ^HttpReturnCode^)]";

        public static string RowFilteringCode =
@"				if (^FilteringFieldCamelCase^ != null) rows = rows.Where(r => r.^FilteringField^ == ^FilteringFieldCamelCase^).ToList();";
    }
}