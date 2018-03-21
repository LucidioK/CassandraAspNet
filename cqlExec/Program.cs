﻿using Cassandra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace cqlExec
{
    static class Program
    {
        static void Main(string[] args)
        {
            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 1 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Console.WriteLine(@"


cqlExec cqlStatement
Example:
cqlExec ""select * from microservices.budget_bill_activity;""

The configuration is defined in file localConfiguration.json, at the same directory as the executable.
");
                Environment.Exit(1);
            }

            var session = OpenCassandraSessionFromLocalSettings("localConfiguration.json");

            var rowSet = session.Execute(args[0]);
            var columnNames = rowSet.Columns.Select(c => c.Name).ToArray();
            var rowSetForJson = new List<object>();
            foreach (var row in rowSet)
            {
                dynamic jsonRow = new ExpandoObject();
                for (int i = 0; i < row.Count(); i++)
                {
                    AddExpandoProperty(jsonRow, columnNames[i], row[i]);
                }
                rowSetForJson.Add(jsonRow);
            }
            var json = JsonConvert.SerializeObject(rowSetForJson, Formatting.Indented);
            Console.WriteLine(json);
        }

        public static void AddExpandoProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public static Session OpenCassandraSessionFromLocalSettings(string localSettingsJsonFile)
        {
            var localSettings = LocalSettings.FromJson(File.ReadAllText(localSettingsJsonFile));
            var cluster = CreateCluster(localSettings.CassandraSettings);
            return (Session)cluster.Connect();
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
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var isValid = sslPolicyErrors == SslPolicyErrors.None;

            return isValid;
        }
    }
}
