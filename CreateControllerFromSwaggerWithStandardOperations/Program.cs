using System;
using System.Collections.Generic;
using System.Linq;

namespace CreateControllerFromSwaggerWithStandardOperations
{
    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("CreateControllerFromSwaggerWithStandardOperations start");

            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 6 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var generator = new ControllerFromSwaggerWithStandardOperationsGenerator(args[0], args[1], int.Parse(args[2]), int.Parse(args[3]), args[4], args[5]);
            generator.Generate();
            Utils.Utils.WriteLineGreen("CreateControllerFromSwaggerWithStandardOperations end");
        }

        private static void Explain()
        {
            System.Console.WriteLine(
@"
CreateControllerFromSwaggerWithStandardOperations SwaggerWithStandardOperationsJson connectionStringOrLocalSettingsJsonFile ApiVersion MaxNumberOfRows CSProjFile typeDescriptionsJson

Creates controller classes for swagger file generaged by GenerateSwaggerStandardOperations.

Example:
    CreateCassandraDBFromCode c:\\temp\\classes\\pseSwaggerWithOps.json ""Contact Points = localhost; Port = 9042"" 1 24 c:\\temp\\classes\\pse.csproj c:\\temp\\classes\\typeDescriptions.json
");

        }
    }
}
