using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cassandra;
namespace Utils
{
    public static class CassandraUtils
    {
        public static Session OpenCassandraSession(string connectionStringOrLocalSettingsJsonFile) =>
            (Utils.InvariantContains(connectionStringOrLocalSettingsJsonFile, "Contact Points") &&
            connectionStringOrLocalSettingsJsonFile.Contains("="))
                ? OpenCassandraSessionWithConnectionString(connectionStringOrLocalSettingsJsonFile)
                : OpenCassandraSessionFromLocalSettings(connectionStringOrLocalSettingsJsonFile);


        public static Session OpenCassandraSessionWithConnectionString(string connectionString)
              => (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();

        public static Session OpenCassandraSessionFromLocalSettings(string localSettingsJsonFile) 
        {
            var localSettings = LocalSettings.FromJson(File.ReadAllText(localSettingsJsonFile));
            var cluster = CreateCluster(localSettings.CassandraSettings);
            return (Session)cluster.Connect();
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var isValid = sslPolicyErrors == SslPolicyErrors.None;

            return isValid;
        }

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
