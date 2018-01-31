﻿using System;
namespace GenerateTypes2SwaggerCall
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;


    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("GenerateTypes2SwaggerCall start");
            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != 3 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {
                Explain();
                Environment.Exit(1);
            }
            var assembly = Assembly.LoadFrom(args[1]);
            var entitiesNamespace = Utils.Utils.CSharpifyName(args[0]) + ".Entities";
            var tableAttribute = typeof(global::Cassandra.Mapping.Attributes.TableAttribute);
            var types = assembly
                .GetExportedTypes()
                .Where(t => 
                    t.Namespace.Equals(entitiesNamespace, StringComparison.InvariantCultureIgnoreCase) &&
                    t.CustomAttributes.Any(a => a.AttributeType == tableAttribute))
                .ToList();
            var classes = new StringBuilder();
            foreach (var type in types)
            {
                classes.Append(classes.Length > 0 ? "," : "");
                classes.Append(type.FullName);
            }
            var command = $"nswag types2swagger /Assembly:{args[1]} /ClassNames:{classes}  /DefaultPropertyNameHandling:CamelCase /DefaultEnumHandling:String /Output:swaggerBase.json";
            File.WriteAllText(args[2], command);
            Utils.Utils.WriteLineGreen("GenerateTypes2SwaggerCall end");
        }
        // nswag types2swagger /Assembly:bin\x64\Release\netcoreapp2.0\win10-x64\publish\%2.dll /ClassNames:PSE.Entities.ContractAccount,PSE.Entities.PaymentHistory /DefaultPropertyNameHandling:CamelCase /DefaultEnumHandling:String /Output:pseSwagger.json
        private static void Explain()
        {
            System.Console.WriteLine("GenerateTypes2SwaggerCall KeySpaceName pathToDll pathToCmdFileToGenerate");
            System.Console.WriteLine("TypeDescriptorsJson is generated by CassandraDBtoCSharp.");
            System.Console.WriteLine("Example:");
            System.Console.WriteLine(@"dotnet tools\GenerateTypes2SwaggerCall.dll pse C:\temp\classes\bin\x64\Debug\netcoreapp2.0\win10-x64\publish\pse.dll C:\temp\classes\Types2Swagger.cmd");
        }
    }
}
