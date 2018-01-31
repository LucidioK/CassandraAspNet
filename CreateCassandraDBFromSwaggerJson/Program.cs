using System;

namespace CreateCassandraDBFromSwaggerJson
{
    class Program
    {
        static void Main(string[] args)
        {
            Utils.Utils.WriteLineGreen("CreateCassandraDBFromSwaggerJson start");
            Utils.Utils.ShowHelpAndAbortIfNeeded(args, 2, @"


CreateCassandraDBFromSwaggerJson cassandraConnectionString swaggerJsonFile

Creates tables for a given Swagger file in a Cassandra DB.
The KeyNameSpace will be the Swagger file name, without the extension.

Example:

TestCassandraConnectionString ""Contact Points = localhost; Port = 9042"" PetStore.json


");
            var gen = new CassandraDBFromSwaggerGenerator(args[0], args[1]);
            gen.Generate();
            Utils.Utils.WriteLineGreen("CreateCassandraDBFromSwaggerJson end");
        }
    }
}
