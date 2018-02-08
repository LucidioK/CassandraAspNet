using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CreateCassandraDBFromCode
{

    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("CreateCassandraDBFromCode start");
            var currentPath = Environment.CurrentDirectory;
            try
            {
                var helpMarkers = new List<string>() { "-?", "?", "--?", "/?", "help", "-help", "--help", "-h", "--h" };
                if (args.Length != 3 || args.Any(a => helpMarkers.Contains(a.ToLowerInvariant())))
                {
                    Explain();
                    Environment.Exit(1);
                }
                var fullAssemblyPath = Path.GetFullPath(args[1]);
                var assemblyDirectory = Path.GetDirectoryName(fullAssemblyPath);
                var assemblyFileName = Path.GetFileName(args[1]);
                
                Directory.SetCurrentDirectory(assemblyDirectory);
                var xuxu = Environment.CurrentDirectory;
                var assembly = Assembly.LoadFrom(fullAssemblyPath);
                var ccc = new CassandraTableCreator();
                ccc.Initialize(args[0], args[2], assembly);
                ccc.Generate();
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception:");
                while (e != null)
                {
                    System.Console.WriteLine(e.Message);
                    e = e.InnerException;
                }
            }
            finally
            {
                Environment.CurrentDirectory = currentPath;
                Utils.Utils.WriteLineGreen("CreateCassandraDBFromCode end");
            }
        }

        private static void Explain()
        {
            System.Console.WriteLine(
@"
CreateCassandraDBFromCode connectionStringOrLocalSettingsJsonFile AssemblyPath KeySpaceName

 Creates a Cassandra DB from a .Net assembly.

Example:
    CreateCassandraDBFromCode ""Contact Points = localhost; Port = 9042"" C:\dsv\MyApp\src\bin\Debug\netcoreapp2.0\win10-x64\publish\App.dll pse

Important:
    The KeySpaceName must be the beginning of the namespace of the types to be converted.
    In the example above, the KeySpaceName is pse, so CreateCassandraDBFromCode will inspect
    only classes with namespace that starts with pse (case insensitive).

    All dependencies for the Assembly must be available either in the same folder or at the Path.
    As such, if you are trying to use a .Net Core assembly, do this:
    1. Go to the folder where your solution is.
    2. dotnet build --runtime win10-x64
    3. dotnet publish --self-contained --runtime win10-x64
    4. Use the 'publish' folder when you refer to the assembly file.
");

        }
    }
}
