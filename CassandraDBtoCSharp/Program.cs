using System;
using System.Collections.Generic;
using System.Linq;

namespace CassandraDBtoCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("CassandraDBtoCSharp start");

            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 3 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var generator = new CSharpGeneratorFromCassandraDB(args[0], args[1], args[2]);
            generator.Generate();
            Utils.Utils.WriteLineGreen("CassandraDBtoCSharp end");
        }

        private static void Explain()
        {
            System.Console.WriteLine(
@"
CassandraDBtoCSharp %ConnectionString% %KeySpaceName% %OutputDirectory%

Creates model classes from Cassandra DB, .

Example:
    CreateCassandraDBFromCode ""Contact Points = localhost; Port = 9042"" pse c:\\temp\\classes
");

        }
    }
}
