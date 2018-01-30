using Newtonsoft.Json;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public class ColumnDescription
    {
        public string CamelCaseName;
        public bool IsPartitionKey = false;
        public bool IsClusteringKey = false;
        public bool IsIndex = false;
        public bool IsFrozen = false;
        public string CassandraType;
        public string CSharpType;
        public string CSharpName;
    }
    public class TypeDescription
    {
        public string CSharpName;
        public string CassandraTableName;
        public List<ColumnDescription> ColumnDescriptions = new List<ColumnDescription>();
    }

    public static class Utils
    {
        public static bool InvariantEquals(string s1, string s2) => 
            string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
        public static bool InvariantStartsWith(string str, string starts) => 
            str.StartsWith(starts, StringComparison.InvariantCultureIgnoreCase);

        public static SwaggerRoot LoadSwagger(string swaggerJsonFileName)
        {
            var swaggerInputText = PreprocessSwaggerFile(File.ReadAllText(swaggerJsonFileName));
            var obj = new object();
            SwaggerRoot swaggerRoot;
            try
            {
                swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(swaggerInputText);
            }
            catch (JsonSerializationException)
            {
                if (Swagger.ObjectModel.SimpleJson.TryDeserializeObject(swaggerInputText, out obj))
                {
                    var jso = (JsonObject)obj;
                    var security = jso.Keys.Contains("security") ? (JsonArray)jso["security"] : null;
                    if (security != null)
                    {
                        jso.Remove("security");
                    }
                    var ss = JsonConvert.SerializeObject(jso);
                    swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(ss);
                    if (security != null)
                    {
                        ReinjectSecurity(swaggerRoot, security);
                    }
                }
                else
                {
                    throw new Exception($"Could not deserialize {swaggerJsonFileName}");
                }
            }
            return swaggerRoot;
        }

        private static void ReinjectSecurity(SwaggerRoot swaggerRoot, JsonArray security)
        {
            swaggerRoot.Security = new Dictionary<SecuritySchemes, IEnumerable<string>>();
            foreach (JsonObject sec in security)
            {
                foreach (var securitySchemeName in sec.Keys)
                {
                    var securityScheme = (SecuritySchemes)Enum.Parse(typeof(SecuritySchemes), securitySchemeName);
                    var permissions = (JsonArray)sec[securitySchemeName];
                    var permissionsList = new List<string>();
                    permissions.ForEach(p => permissionsList.Add((string)p));
                    swaggerRoot.Security.Add(securityScheme, permissionsList);
                }
            }
        }

        public static void ShowHelpAndAbortIfNeeded(string[] args, int expectedNumberOfArgs, string helpMessage)
        {
            var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
            if (args.Length != expectedNumberOfArgs || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
            {

                Environment.Exit(1);
            }
        }

        public static string CassandrifyName(string name)
        {
            var cassandrified = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsUpper(c) && cassandrified.Length > 0)
                {
                    cassandrified.Append('_');
                }
                cassandrified.Append(char.ToLowerInvariant(c));
            }
            return cassandrified.ToString();
        }

        public static string CamelCase(string name)
        {
            var cSharpified = CSharpifyName(name);
            return cSharpified.Substring(0, 1).ToLowerInvariant() + cSharpified.Substring(1);
        }
        public static string CSharpifyName(string name)
        {
            var cassandrified = new StringBuilder();
            var previousWasUnderline = false;
            foreach (var c in name)
            {
                if (c == '_')
                {
                    previousWasUnderline = true;
                    continue;
                }
                cassandrified.Append(
                    cassandrified.Length == 0 || previousWasUnderline ?
                        char.ToUpperInvariant(c) :
                        c);
                previousWasUnderline = false;
            }
            return cassandrified.ToString();
        }

        public static string PreprocessSwaggerFile(string swaggerJson)
        {
            var processed = swaggerJson.Replace("\n", "").Replace("\r", "").Replace("\t", " ").Replace("formData", "form");
            processed = Regex.Replace(processed, " +", " ");
            processed = Regex.Replace(processed, "\\[ \"null\" *, *(\"[a-z]+\") *]", m => m.Groups[1].Value);
            System.Diagnostics.Debug.WriteLine(processed);
            return processed;
        }

        public static void WriteLine(string str, ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(str);
            System.Console.ResetColor();
        }

        public static void WriteLineRed(string str)
        {
            WriteLine(str, ConsoleColor.Red);
        }
        public static void WriteLineGreen(string str)
        {
            WriteLine(str, ConsoleColor.Green);
        }
        public static void WriteLineYellow(string str)
        {
            WriteLine(str, ConsoleColor.Yellow);
        }
        public static void WriteLineBlue(string str)
        {
            WriteLine(str, ConsoleColor.Blue);
        }

        public static T Clone<T>(T obj)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }
    }
}
