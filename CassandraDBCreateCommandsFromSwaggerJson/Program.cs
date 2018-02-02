using System;

namespace CassandraDBCreateCommandsFromSwaggerJson
{
    class Program
    {
        static void Main(string[] args)
        {
            var cs = new CassandraDBFromSwagger.CassandraDBFromSwagger(args[0]);
            foreach (var c in cs.Generate())
            {
                Console.WriteLine(c);
            }
        }
    }
}
