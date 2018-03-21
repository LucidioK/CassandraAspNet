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
            if (args.Length < 3 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var materializedViewNames = new List<string>();
            for (var i = 3; i < args.Length; i++)
            {
                materializedViewNames.Add(args[i]);
            }
            var generator = new CSharpGeneratorFromCassandraDB(args[0], args[1], args[2], materializedViewNames);
            generator.Generate();
            Utils.Utils.WriteLineGreen("CassandraDBtoCSharp end");
        }

        private static void Explain()
        {
            System.Console.WriteLine(
@"
CassandraDBtoCSharp connectionStringOrLocalSettingsJsonFile KeySpaceName OutputDirectory [materializedView1...]

Creates model classes from Cassandra DB.

Examples:
    CreateCassandraDBFromCode ""Contact Points = localhost; Port = 9042"" pse c:\\temp\\classes
    CreateCassandraDBFromCode C:\\dsv\\authentication\\src\\localConfiguration.json selfservice_auth c:\\Temp\\selfservice_auth
    CreateCassandraDBFromCode C:\\dsv\\authentication\\src\\localConfiguration.json selfservice_auth c:\\Temp\\selfservice_auth user_auth_by_name user_by_bp
");

        }
    }
}
