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
            var swaggerRoot = Utils.Utils.LoadSwagger(this.swaggerFileName);
            var session = (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();
            var keySpaceName = Path.GetFileNameWithoutExtension(this.swaggerFileName).ToLowerInvariant();
            session.CreateKeyspaceIfNotExists(keySpaceName);
            CreateUserDefinedTypesIfNeeded(keySpaceName, swaggerRoot, session);
            foreach (var entity in swaggerRoot.Definitions)
            {
                var entityName = entity.Key;
                if (userDefinedTypes.Contains(entityName))
                {
                    continue;
                }
                CreateTypeOrTable("table", keySpaceName, swaggerRoot, session, entityName);
            }
        }

        private static string GetColumnDefinitions(string itemType, IDictionary<string, Schema> columns)
        {
            var sb = new StringBuilder();
            foreach (var propertyName in columns.Keys)
            {
                var property = columns[propertyName];
                var propertyType = GetCassandraType(property);
                if (propertyType != null)
                {
                    sb.Append(sb.Length > 0 ? "," : "");
                    sb.Append($"{ Utils.Utils.CassandrifyName(propertyName)} {propertyType}");
                    if (Utils.Utils.InvariantEquals(itemType, "table") && Utils.Utils.InvariantEquals(propertyName, "id"))
                    {
                        sb.Append(" primary key");
                    }
                }
            }
            return $"({sb.ToString()})";
        }

        private static void CreateUserDefinedTypesIfNeeded(string keyspace, SwaggerRoot swaggerRoot, Session session)
        {
            foreach (var entity in swaggerRoot.Definitions)
            {
                var entityName = entity.Key;
                foreach (var propertyName in entity.Value.Properties.Keys)
                {
                    var property = entity.Value.Properties[propertyName];
                    var udtName = property?.Ref ?? property?.Items?.Ref;
                    if (udtName != null)
                    {
                        udtName = GetRefname(udtName);
                        userDefinedTypes.Add(udtName);
                        CreateTypeOrTable("type", keyspace, swaggerRoot, session, udtName);
                    }
                }

            }
        }

        private static string GetRefname(string udtName)
        {
            if (udtName.Contains("/"))
            {
                udtName = udtName.Split('/').ToList().Last();
            }

            return udtName;
        }

        private static void CreateTypeOrTable(string itemType, string keyspace, SwaggerRoot swaggerRoot, Session session, string itemName)
        {
            var itemNameCassandrified = Utils.Utils.CassandrifyName(itemName);
            var columnDefinitions = GetColumnDefinitions(itemType, swaggerRoot.Definitions[itemName].Properties);
            var cql = $"create {itemType} if not exists {keyspace}.{itemNameCassandrified} {columnDefinitions} ;";
            Utils.Utils.WriteLineGreen($"Trying to create {itemType} {itemNameCassandrified} from Swagger definition {itemName}.");
            try
            {
                session.Execute(cql);
            }
            catch(Exception e)
            {
                Utils.Utils.ExceptionConsoleDisplay(e, $"Failed to create {itemType} {itemNameCassandrified} from Swagger definition {itemName}.");
            }
        }

        private static string GetCassandraType(Schema schema)
        {
            return GetCassandraType(schema.Type, schema.Format, schema.Items, schema.Ref);

        }

        private static string GetCassandraType(string type, string format, Item items, string reference)
        {
            if (format == null && type == "string")
            {
                return "text";
            }
            if (type?.ToLowerInvariant() == "array")
            {
                return (items.Ref != null) ? 
                    $"frozen<list<{Utils.Utils.CassandrifyName(GetRefname(items.Ref))}>>" 
                    : 
                    $"frozen<list<{GetCassandraType(items.Type, items.Format, items.Items, items.Ref)}>>";
            }
            if (type?.ToLowerInvariant() == "boolean")
            {
                return "boolean";
            }
            if (reference != null)
            {
                return Utils.Utils.CassandrifyName(GetRefname(reference));
            }
            switch (format?.ToLowerInvariant())
            {
                case "int64":
                    return "bigint";
                case "int32":
                    return "int";
                case "int16":
                    return "int";
                case "int8":
                    return "int";
                case "date-time":
                    return "timestamp";
                default:
                    return null;
            }
        }
    }
}
