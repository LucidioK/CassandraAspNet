using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CassandraDBFromSwagger
{
    public class CassandraDBFromSwagger
    {
        string swaggerFileName;
        static List<string> userDefinedTypes = new List<string>();
        List<string> cqlStatements = new List<string>();
        public CassandraDBFromSwagger(string swaggerFileName)
        {
            this.swaggerFileName = swaggerFileName;
        }

        public List<string> Generate()
        {
            var swaggerRoot = Utils.Utils.LoadSwagger(this.swaggerFileName);
            var keySpaceName = Path.GetFileNameWithoutExtension(this.swaggerFileName).ToLowerInvariant();
            this.cqlStatements.Add($"create keyspace if not exists {keySpaceName} WITH REPLICATION = {{ 'class': 'SimpleStrategy', 'replication_factor': 3 }};");
            CreateUserDefinedTypesIfNeeded(keySpaceName, swaggerRoot);
            foreach (var entity in swaggerRoot.Definitions)
            {
                var entityName = entity.Key;
                if (userDefinedTypes.Contains(entityName))
                {
                    continue;
                }
                CreateTypeOrTable("table", keySpaceName, swaggerRoot, entityName);
            }
            return cqlStatements;
        }

        private string GetColumnDefinitions(string itemType, IDictionary<string, Schema> columns)
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

        private void CreateUserDefinedTypesIfNeeded(string keyspace, SwaggerRoot swaggerRoot)
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
                        CreateTypeOrTable("type", keyspace, swaggerRoot, udtName);
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

        private void CreateTypeOrTable(string itemType, string keyspace, SwaggerRoot swaggerRoot, string itemName)
        {
            var itemNameCassandrified = Utils.Utils.CassandrifyName(itemName);
            var columnDefinitions = GetColumnDefinitions(itemType, swaggerRoot.Definitions[itemName].Properties);
            var cql = $"create {itemType} if not exists {keyspace}.{itemNameCassandrified} {columnDefinitions} ;";
            this.cqlStatements.Add(cql);
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
