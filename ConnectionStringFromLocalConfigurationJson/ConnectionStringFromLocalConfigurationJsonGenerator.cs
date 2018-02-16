using Newtonsoft.Json;
using Cassandra;
using System;
using System.IO;
using System.Linq;

namespace ConnectionStringFromLocalConfigurationJson
{
    internal class ConnectionStringFromLocalConfigurationJsonGenerator
    {
        private string _localConfigurationFile;
        private string _keySpaceName;

        public ConnectionStringFromLocalConfigurationJsonGenerator(string keySpaceName, string localConfigurationFile)
        {
            _localConfigurationFile = localConfigurationFile;
            _keySpaceName = keySpaceName;
        }

        internal string Generate()
        {
            var localConfiguration = JsonConvert.DeserializeObject<Utils.LocalSettings>(File.ReadAllText(_localConfigurationFile));
            var session = Utils.CassandraUtils.OpenCassandraSessionFromLocalSettings(_localConfigurationFile);
            var cassandraConnectionStringBuilder = new CassandraConnectionStringBuilder();
            cassandraConnectionStringBuilder.ContactPoints = localConfiguration.CassandraSettings.Hosts.Select(h => h.IpAddress).ToArray();
            cassandraConnectionStringBuilder.ClusterName = session.Cluster.Metadata.ClusterName;
            cassandraConnectionStringBuilder.DefaultKeyspace = _keySpaceName;
            cassandraConnectionStringBuilder.Password = localConfiguration.CassandraSettings.ClusterPassword;
            cassandraConnectionStringBuilder.Port = (int)localConfiguration.CassandraSettings.Port;
            cassandraConnectionStringBuilder.Username = localConfiguration.CassandraSettings.ClusterUser;
            return cassandraConnectionStringBuilder.ToString();
        }
    }
}