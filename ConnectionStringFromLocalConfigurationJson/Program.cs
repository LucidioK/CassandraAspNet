using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectionStringFromLocalConfigurationJson
{
    class Program
    {
        static void Main(string[] args)
        {
            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 2 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var generator = new ConnectionStringFromLocalConfigurationJsonGenerator(args[0], args[1]);
            string connectionString = generator.Generate();
            Console.WriteLine(connectionString);
        }

        private static void Explain()
        {
            Utils.Utils.WriteLineBlue(@"
ConnectionStringFromLocalConfigurationJson defaultKeySpaceName localSettingsJsonFile

Displays a connection string from the config in localSettingsJsonFile

Example:
    ConnectionStringFromLocalConfigurationJson microservices C:\dsv\installment\src\localConfiguration.json
");
        }
    }
}
