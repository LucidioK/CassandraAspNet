using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cqlExec
{
    static class Program
    {
        static void Main(string[] args)
        {
            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 2 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Console.WriteLine(@"


cqlExec ConnectionString cqlStatement
Example:
cqlExec ""Contact Points = localhost; Port = 9042"" ""describe keyspaces;""


");
                Environment.Exit(1);
            }
          

            var session = (Session)Cluster.Builder().WithConnectionString(args[0]).Build().Connect();
            var rowSet = session.Execute(args[1]);
            foreach (var row in rowSet)
            {
                bool firstCol = true;
                foreach (var col in row)
                {
                    if (!firstCol)
                    {
                        Console.Write("|");
                    }
                    Console.Write(col.ToString());
                    firstCol = false;
                }
            }

        }
    }
}
