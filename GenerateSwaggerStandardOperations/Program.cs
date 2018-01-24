

namespace GenerateSwaggerStandardOperations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {

        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("GenerateSwaggerStandardOperations start");

            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 5 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var generator = new SwaggerStandardOperationsGenerator(args[0], args[1], args[2], args[3], args[4]);
            generator.Generate();
            Utils.Utils.WriteLineGreen("GenerateSwaggerStandardOperations end");
        }

        private static void Explain()
        {
            System.Console.WriteLine(
@"
GenerateSwaggerStandardOperations KeyNameSpace SwaggerFromNSwagJson typeDescriptionsJson OutputSwaggerJson OAuth2URL

Injects GET request formats into Swagger file generated from NSwag.

Example:
    CreateCassandraDBFromCode pse c:\\temp\\classes\\swaggerBase.json c:\\temp\\classes\\typeDescriptions.json c:\\temp\\classes\\swagger.json  https://somesite.com/oauth/authorize/?client_id=CLIENT-ID&redirect_uri=REDIRECT-URI&response_type=token
");

        }
    }
}
