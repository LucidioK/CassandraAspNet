using System;
using System.Collections.Generic;
using System.Text;

namespace cqlExec
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Cassandra;

    public partial class LocalSettings
    {
        [JsonProperty("IdentityProviders")]
        public IdentityProvider[] IdentityProviders { get; set; }

        [JsonProperty("CassandraSettings")]
        public CassandraSettings CassandraSettings { get; set; }
    }

    public partial class CassandraSettings
    {
        [JsonProperty("Hosts")]
        public Host[] Hosts { get; set; }

        [JsonProperty("Port")]
        public long Port { get; set; }

        [JsonProperty("ClusterUser")]
        public string ClusterUser { get; set; }

        [JsonProperty("ClusterPassword")]
        public string ClusterPassword { get; set; }

        [JsonProperty("ConsistencyLevel")]
        public ConsistencyLevel ConsistencyLevel { get; set; }

        [JsonProperty("UseClusterCredentials")]
        public bool UseClusterCredentials { get; set; }

        [JsonProperty("UseSSL")]
        public bool UseSsl { get; set; }

        [JsonProperty("UseQueryOptions")]
        public bool UseQueryOptions { get; set; }

        [JsonProperty("MaxConnectionsPerHost")]
        public int MaxConnectionsPerHost { get; set; }
    }

    public partial class Host
    {
        [JsonProperty("HostName")]
        public string HostName { get; set; }

        [JsonProperty("IpAddress")]
        public string IpAddress { get; set; }
    }

    public partial class IdentityProvider
    {
        [JsonProperty("Issuer")]
        public string Issuer { get; set; }

        [JsonProperty("Modulus")]
        public string Modulus { get; set; }

        [JsonProperty("Expo")]
        public string Expo { get; set; }
    }

    public partial class LocalSettings
    {
        public static LocalSettings FromJson(string json) => JsonConvert.DeserializeObject<LocalSettings>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this LocalSettings self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
