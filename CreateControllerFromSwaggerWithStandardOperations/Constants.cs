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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cassandra;
using Newtonsoft.Json;

namespace ^NamespaceBase^
{
    public partial class CassandraSettings
    {
        public Host[] Hosts { get; set; }
        public long Port { get; set; }
        public string ClusterUser { get; set; }
        public string ClusterPassword { get; set; }
        public ConsistencyLevel ConsistencyLevel { get; set; }
        public bool UseClusterCredentials { get; set; }
        public bool UseSsl { get; set; }
        public bool UseQueryOptions { get; set; }
        public int MaxConnectionsPerHost { get; set; }
    }

    public partial class Host
    {
        public string HostName { get; set; }
        public string IpAddress { get; set; }
    }

    public partial class IdentityProvider
    {
        public string Issuer { get; set; }
        public string Modulus { get; set; }
        public string Expo { get; set; }
    }

    public class Constants
    {	  
		public static string connectionStringOrCassandraSettingsJsonContent = @""^connectionStringOrCassandraSettingsJsonContent^"";

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

        public static Session OpenCassandraSession() =>
            connectionStringOrCassandraSettingsJsonContent.Contains(""Contact Points"") &&
            connectionStringOrCassandraSettingsJsonContent.Contains(""="")
                ? OpenCassandraSessionWithConnectionString(connectionStringOrCassandraSettingsJsonContent)
                : OpenCassandraSessionFromLocalSettings(connectionStringOrCassandraSettingsJsonContent);


        public static Session OpenCassandraSessionWithConnectionString(string connectionString) => 
            (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();

        public static Session OpenCassandraSessionFromLocalSettings(string cassandraSettings) =>
            (Session)CreateCluster(JsonConvert.DeserializeObject<CassandraSettings>(cassandraSettings)).Connect();

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            sslPolicyErrors == SslPolicyErrors.None;

        private static Cluster CreateCluster(CassandraSettings settings)
        {
            var poolingOptions = PoolingOptions.Create();
            poolingOptions.SetMaxConnectionsPerHost(HostDistance.Remote, settings.MaxConnectionsPerHost);
            var builder = Cluster.Builder().WithPoolingOptions(poolingOptions);
            var hosts = settings.Hosts;

            if (settings.UseSsl)
            {
                var sslOptions = new SSLOptions();
                sslOptions.SetCertificateRevocationCheck(false);
                sslOptions.SetRemoteCertValidationCallback(ValidateServerCertificate);
                sslOptions.SetHostNameResolver((Func<IPAddress, string>)(internalIpAddress =>
                {
                    var host = hosts.FirstOrDefault<Host>((Func<Host, bool>)(o => o.IpAddress == internalIpAddress.ToString()));
                    if (host != null && !string.IsNullOrWhiteSpace(host.HostName))
                        return host.HostName;
                    return internalIpAddress.ToString();
                }));

                builder = builder.WithSSL(sslOptions);
            }

            if (settings.UseClusterCredentials)
            {
                builder = builder.WithCredentials(settings.ClusterUser, settings.ClusterPassword);
            }

            if (settings.UseQueryOptions)
            {
                var queryOptions = new QueryOptions();
                queryOptions.SetConsistencyLevel(settings.ConsistencyLevel);
                builder = builder.WithQueryOptions(queryOptions);
            }

            var cluster = builder.AddContactPoints(hosts.Select<Host, string>((Func<Host, string>)(o => o.IpAddress))).Build();

            return cluster;
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
	using ^NamespaceBase^.Model;
	using Cassandra;
	using Cassandra.Data.Linq;
    using System.Linq;

    //[ApiVersion(""^ApiVersion^"")]
    [Produces(""application/json"")]
    [Route(""v^ApiVersion^"")]
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
			_session = Constants.OpenCassandraSession();
            UdtMapping.Map(_session);
        }
		
^ProducesResponseAttributes^
        [HttpGet(""^EntityNameCamelCase^"")]
        public async Task<IActionResult> Get^EntityName^(^FilterParameters^)
        {
			return await Get(null^FilterParametersNamesOnlyWithPrecedingComa^);
		}

		
^ProducesResponseAttributes^
        [HttpGet(""^EntityNameCamelCase^/{id}"")]
        public async Task<IActionResult> Get^EntityName^ById(^PrimaryKeyColumnType^ id^FilterParametersWithPrecedingComa^)
        {
			return await Get(id^FilterParametersNamesOnlyWithPrecedingComa^);
		}
		
		private async Task<IActionResult> Get(^PrimaryKeyColumnType^ id^FilterParametersWithPrecedingComa^)
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