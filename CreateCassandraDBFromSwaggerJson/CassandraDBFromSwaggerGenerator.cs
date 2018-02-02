using Cassandra;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreateCassandraDBFromSwaggerJson
{
    class CassandraDBFromSwaggerGenerator
    {
        string connectionString;
        string swaggerFileName;
        static List<string> userDefinedTypes = new List<string>();
        public CassandraDBFromSwaggerGenerator(string connectionString, string swaggerFileName)
        {
            this.connectionString = connectionString;
            this.swaggerFileName = swaggerFileName;
        }

        public void Generate()
        {
            var session = (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();
            var cassandraDBFromSwagger = new CassandraDBFromSwagger.CassandraDBFromSwagger(this.swaggerFileName);
            var cqlStatements = cassandraDBFromSwagger.Generate();
            foreach (var cqlStatement in cqlStatements)
            {
                session.Execute(cqlStatement);
            }

        }
    }
}
