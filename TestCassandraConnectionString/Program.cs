
namespace TestCassandraConnectionString
{
    using System;
    using Cassandra;
    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("TestCassandraConnectionString start");
            Utils.Utils.ShowHelpAndAbortIfNeeded(args, 1, @"


TestCassandraConnectionString cassandraConnectionString
Tests whether we can connect to the given connection string.
App will set errorlevel 0 in case of success, 1 in case of failure.
Example:
TestCassandraConnectionString ""Contact Points = localhost; Port = 9042""


");
            try
            {
                var session = (Session)Cluster.Builder().WithConnectionString(args[0]).Build().Connect();
                var keyspaces = session.Cluster.Metadata.GetKeyspaces();
            }
            catch (Exception e)
            {
                Utils.Utils.ExceptionExit(e, $"Could not connect to Cassandra with connection string[{args[0]}]", 1);
            }
        }
    }
}
