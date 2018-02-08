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
        string connectionStringOrLocalSettingsJsonFile;
        string swaggerFileName;
        static List<string> userDefinedTypes = new List<string>();
        public CassandraDBFromSwaggerGenerator(string connectionStringOrLocalSettingsJsonFile, string swaggerFileName)
        {
            this.connectionStringOrLocalSettingsJsonFile = connectionStringOrLocalSettingsJsonFile;
            this.swaggerFileName = swaggerFileName;
        }

        public void Generate()
        {
            var session = Utils.CassandraUtils.OpenCassandraSession(connectionStringOrLocalSettingsJsonFile);
            var b = new Builder();
            var cassandraDBFromSwagger = new CassandraDBFromSwagger.CassandraDBFromSwagger(this.swaggerFileName);
            var cqlStatements = cassandraDBFromSwagger.Generate();
            foreach (var cqlStatement in cqlStatements)
            {
                session.Execute(cqlStatement);
            }

        }
    }
}
