using Cassandra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

OR

cqlExec DESCRIBE

Using DESCRIBE, the tool will list all items from the Cassandra DB.
The configuration is defined in file localConfiguration.json, at the same directory as the executable.
");
                Environment.Exit(1);
            }
            var configFilepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localConfiguration.json");

            var session = OpenCassandraSessionFromLocalSettings(configFilepath);

            if (args[0].ToUpperInvariant() == "DESCRIBE")
            {
                Describe(session);
            }
            else
            {
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
        }

        private static void Describe(Session session)
        {
/*
            var json = JsonConvert.SerializeObject(session.Cluster.Metadata, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented,
            });
            Console.WriteLine(json);
*/            
            var description = new ExpandoObject();
            var keySpaces = session.Cluster.Metadata.GetKeyspaces().ToList();
            keySpaces.Sort();
            foreach (var keySpace in keySpaces)
            {
                var keyspaceExpando = new ExpandoObject();
                var tableNames = session.Cluster.Metadata.GetTables(keySpace).ToList();
                tableNames.Sort();
                foreach (var tableName in tableNames)
                {
                    keyspaceExpando.TryAdd(tableName, session.Cluster.Metadata.GetTable(keySpace, tableName));
                }
                description.TryAdd(keySpace, keyspaceExpando);
            }
            var json = JsonConvert.SerializeObject(description,  new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            Formatting = Formatting.Indented,
                        });
            Console.WriteLine(json);

            /*
                        var fullDescription = new JObject();
                        var settings =new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            Formatting = Formatting.Indented,
                        };
                        var keySpaces = session.Cluster.Metadata.GetKeyspaces().ToList();
                        foreach (var keySpace in keySpaces)
                        {
                            var keySpaceObject = new JObject();
                            fullDescription.Add(keySpace, keySpaceObject);
                            var tablesObject = new JObject();
                            keySpaceObject.Add("tables", tablesObject);
                            foreach (var tableName in session.Cluster.Metadata.GetTables(keySpace))
                            {
                                var table = (DataCollectionMetadata)session.Cluster.Metadata.GetTable(keySpace, tableName);

                                var tableJson = JsonConvert.SerializeObject(table, settings);
                                tablesObject.Add(tableName, tableJson);
                            }

                        }
                        var description = fullDescription
                            .ToString(Formatting.Indented, null)
                            .Replace("\\r", "\r")
                            .Replace("\\n", "\n")
                            .Replace("\\\"", "\"");
                        Console.WriteLine(description);
            */
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
