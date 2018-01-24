using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public class ColumnDescription
    {
        public string CamelCaseName;
        public bool IsPartitionKey = false;
        public bool IsClusteringKey = false;
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
            var processed = swaggerJson.Replace("\n", "").Replace("\r", "").Replace("\t", " ");
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
    }
}
