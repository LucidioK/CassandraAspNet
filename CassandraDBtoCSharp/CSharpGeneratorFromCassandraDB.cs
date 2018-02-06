using Cassandra;
using Newtonsoft.Json;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utils;

namespace CassandraDBtoCSharp
{
    internal class ColumnDescOrTableColumn
    {
        internal ColumnDesc columnDesc;
        internal TableColumn tableColumn;
        public static implicit operator ColumnDescOrTableColumn(ColumnDesc columnDesc) => 
            new ColumnDescOrTableColumn
            {
                columnDesc = columnDesc
            };

        public static implicit operator ColumnDescOrTableColumn(TableColumn tableColumn) =>
            new ColumnDescOrTableColumn
            {
                tableColumn = tableColumn
            };

        public string Keyspace => columnDesc != null ? columnDesc.Keyspace : tableColumn.Keyspace;
        public string Name => columnDesc != null ? columnDesc.Name : tableColumn.Name;
        public string Table => columnDesc != null ? columnDesc.Table : tableColumn.Table;
        public ColumnTypeCode TypeCode => columnDesc != null ? columnDesc.TypeCode : tableColumn.TypeCode;
        public IColumnInfo TypeInfo => columnDesc != null ? columnDesc.TypeInfo : tableColumn.TypeInfo;
    }

    internal class CSharpGeneratorFromCassandraDB
    {
        private List<Utils.TypeDescription> typeDescriptions = new List<Utils.TypeDescription>();
        private List<string> log = new List<string>();
        public List<string> Log => log;
        private Session session;
        private string keySpaceName;
        private string outputDirectory;
        private string modelDirectory;
        private List<string> udtClasses = new List<string>();
        private SwaggerRoot swaggerRoot = new SwaggerRoot();
        private string csproj = @"
<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <AssemblyName>App</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include='CassandraCSharpDriver' Version='3.4.0.1' />
    <PackageReference Include='Microsoft.AspNetCore.All' Version='2.0.3' />
    <PackageReference Include='Microsoft.Extensions.DependencyInjection.Abstractions' Version='2.0.0' />
    <PackageReference Include='Microsoft.VisualStudio.Web.CodeGeneration.Design' Version='2.0.1' />
    <PackageReference Include='Newtonsoft.Json' Version='10.0.1' />
    <PackageReference Include='Ninject' Version='3.3.4' />
    <PackageReference Include='Swashbuckle.AspNetCore' Version='1.1.0' />
    <PackageReference Include='System.IdentityModel.Tokens.Jwt' Version='5.1.5' />
  </ItemGroup>
  <ItemGroup>
    <Content Update='swagger.json'>
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
";

        private Dictionary<Cassandra.ColumnTypeCode, Func<ColumnDescOrTableColumn, string>> cassandraToCSharpTypeEquivalency;
        private bool isFrozen = false;

        public CSharpGeneratorFromCassandraDB(string connectionString, string keySpaceName, string outputDirectory)
        {
            this.session = (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();
            this.swaggerRoot.Definitions = new Dictionary<string, Schema>();
            this.swaggerRoot.Info = new Info { Title = keySpaceName, Version = "" };
            this.swaggerRoot.Schemes = new List<Schemes>();
            this.swaggerRoot.Paths = new Dictionary<string, PathItem>();
            this.keySpaceName = keySpaceName;
            this.outputDirectory = outputDirectory;
            Utils.Utils.CreateDirectoryIfNeeded(this.outputDirectory);
            this.modelDirectory = Path.Combine(outputDirectory, "Model");
            Utils.Utils.CreateDirectoryIfNeeded(this.modelDirectory);

            this.cassandraToCSharpTypeEquivalency = new Dictionary<ColumnTypeCode, Func<ColumnDescOrTableColumn, string>>()
            {
                { Cassandra.ColumnTypeCode.Ascii     , t => "string" },
                { Cassandra.ColumnTypeCode.Bigint    , t => "long" },
                { Cassandra.ColumnTypeCode.Boolean   , t => "bool" },
                { Cassandra.ColumnTypeCode.Counter   , t => "int" },
                { Cassandra.ColumnTypeCode.Date      , t => "DateTime" },
                { Cassandra.ColumnTypeCode.Decimal   , t => "decimal" },
                { Cassandra.ColumnTypeCode.Double    , t => "double" },
                { Cassandra.ColumnTypeCode.Float     , t => "float" },
                { Cassandra.ColumnTypeCode.Inet      , t => "string" },
                { Cassandra.ColumnTypeCode.Int       , t => "int" },
                { Cassandra.ColumnTypeCode.List      , t => this.GetListTypeName(t) },
                { Cassandra.ColumnTypeCode.SmallInt  , t => "int" },
                { Cassandra.ColumnTypeCode.Text      , t => "string" },
                { Cassandra.ColumnTypeCode.Time      , t => "DateTime" },
                { Cassandra.ColumnTypeCode.Timestamp , t => "DateTime" },
                { Cassandra.ColumnTypeCode.TinyInt   , t => "int" },
                { Cassandra.ColumnTypeCode.Udt       , t => this.GetUDTTypeName(t) },
                { Cassandra.ColumnTypeCode.Uuid      , t => "guid" },
                { Cassandra.ColumnTypeCode.Varchar   , t => "string" },
                { Cassandra.ColumnTypeCode.Varint    , t => "int" },
                //{ Cassandra.ColumnTypeCode.Blob    , null },
                //{ Cassandra.ColumnTypeCode.Timeuuid, null },
                //{ Cassandra.ColumnTypeCode.Map     , null },
                //{ Cassandra.ColumnTypeCode.Set     , null },
                //{ Cassandra.ColumnTypeCode.Tuple   , null },
            };
        }

        internal void Generate()
        {
            if (!Directory.Exists(outputDirectory))
            {
                throw new DirectoryNotFoundException($"{this.outputDirectory} not found.");
            }
            var tables = session.Cluster.Metadata.GetTables(this.keySpaceName.ToLowerInvariant()).ToList();
            var keySpace = session.Cluster.Metadata.GetKeyspace(this.keySpaceName.ToLowerInvariant());

            log.Add($"Starting generation, {tables.Count} tables.");
            foreach (var tableName in tables)
            {
                var tableDef = session.Cluster.Metadata.GetTable(this.keySpaceName.ToLowerInvariant(), tableName);
                var columnDescriptions = new List<Utils.ColumnDescription>();
                var className = Utils.Utils.CSharpifyName(tableDef.Name);
                this.swaggerRoot.Definitions.Add(className, new Schema());
                log.Add($" Starting table {tableDef.Name}, class {className}");
                var properties = new List<string> { "" };
                foreach (var tableColumn in tableDef.TableColumns)
                {
                    this.isFrozen = false;
                    log.Add($" Table {tableDef.Name}, class {className}, column {tableColumn.Name}");
                    var csharpTypeName = this.GetCSharpTypeName(tableColumn);
                    if (csharpTypeName != null)
                    {
                        AddProperty(tableDef, columnDescriptions, properties, tableColumn, csharpTypeName);
                    }
                    else
                    {
                        log.Add($" Table {tableDef.Name}, class {className}, column {tableColumn.Name}: DON'T KNOW HOW TO HANDLE TYPE {tableColumn.TypeCode}");
                    }
                }
                PopulateIsIndexField(columnDescriptions, tableDef);
                this.CreateCSFile(
                    className,
                    properties,
                    "using Cassandra.Mapping.Attributes;",
                    $@"[Table(""{tableDef.Name}"", Keyspace = ""{keySpace.Name}"" )]");
                this.typeDescriptions.Add(
                    new Utils.TypeDescription
                    {
                        CassandraTableName = tableDef.Name,
                        CSharpName = className,
                        ColumnDescriptions = columnDescriptions
                    });
            }
            this.CreateUdtTypeInitializerClass();
            this.CreateSwaggerJson();
            this.CreateTypeDescriptionJson();
            //File.WriteAllText(Path.Combine(this.outputDirectory, this.keySpaceName + ".csproj"), this.csproj);
        }

        private void CreateSwaggerJson()
        {
            var swaggerDefinitions = this.swaggerRoot.Definitions.Keys.ToList();
            foreach (var className in swaggerDefinitions)
            {
                var typeDescription = this.typeDescriptions.FirstOrDefault(t => t.CSharpName == className);
                Schema schema;
                if (this.swaggerRoot.Definitions.TryGetValue(className, out schema))
                {
                    schema.Type = "object";
                    schema.Properties = new Dictionary<string, Schema>();
                    foreach (var columnDescription in typeDescription.ColumnDescriptions)
                    {
                        var propertySchema = new Schema
                        {
                            Type = GetSwaggerType(columnDescription),
                            Ref = GetSwaggerRef(columnDescription),
                            Format = GetSwaggerFormat(columnDescription),
                        };
                        var itemsRef = GetSwaggerItemsRef(columnDescription);
                        var itemsType = GetSwaggerItemsType(columnDescription);
                        if (itemsRef != null || itemsType != null)
                        {
                            propertySchema.Items = new Item { Ref = itemsRef, Type = itemsType };
                        }
                        schema.Properties.Add(Utils.Utils.CamelCase(columnDescription.CSharpName), propertySchema);
                    }
                }
            }
            var jsonFileName = Path.Combine(this.outputDirectory, "swaggerBase.json");
            File.WriteAllText(jsonFileName, swaggerRoot.ToJson());
        }

        private string GetCSharpTypeName(ColumnDescOrTableColumn tableColumn)
        {
            Func<ColumnDescOrTableColumn, string> csharpTypeNameFunc;
            if (this.cassandraToCSharpTypeEquivalency.TryGetValue(tableColumn.TypeCode, out csharpTypeNameFunc))
            {
                return csharpTypeNameFunc(tableColumn);
            }
            return null;
        }

        private void PopulateIsIndexField(List<ColumnDescription> columnDescriptions, TableMetadata tableDef)
        {
            foreach (var index in tableDef.Indexes)
            {
                var indexedPropertyName = Utils.Utils.CSharpifyName(index.Value.Target);
                var indexedProperty = columnDescriptions.FirstOrDefault(c => c.CSharpName == indexedPropertyName);
                if (indexedProperty != null)
                {
                    indexedProperty.IsIndex = true;
                }
            }
        }

        private void AddProperty(
            TableMetadata tableDef, 
            List<Utils.ColumnDescription> columnDescriptions, 
            List<string> properties, 
            TableColumn tableColumn, 
            string csharpTypeName)
        {
            var columnName = tableColumn.Name;
            var columnNameCS = Utils.Utils.CSharpifyName(columnName);
            var columnDescription = new Utils.ColumnDescription
            {
                CassandraColumnName = columnName,
                CamelCaseName = Utils.Utils.CamelCase(columnName),
                CSharpName = columnNameCS,
                IsPartitionKey = tableDef.PartitionKeys.Any(k => k.Name == columnName),
                IsClusteringKey = tableDef.ClusteringKeys.Any(k => k.Item1.Name == columnName),
                CassandraType = tableColumn.TypeCode.ToString(),
                CSharpType = csharpTypeName,
                IsFrozen = this.isFrozen,
            };
            columnDescriptions.Add(columnDescription);
            if (columnDescription.IsPartitionKey)
            {
                properties.Add("        [PartitionKey]");
            }

            if (columnDescription.IsFrozen)
            {
                properties.Add("        [FrozenValue]");
            }

            if (columnDescription.IsClusteringKey)
            {
                properties.Add($"       [ClusteringKey(0, Name = \"{columnName}\")]");
            }
            else
            {
                properties.Add($"        [Column(\"{columnName}\")]");
            }
            properties.Add($"        public {csharpTypeName} {columnNameCS} {{ get; set; }}");
            properties.Add("");
        }

        private void CreateTypeDescriptionJson() => 
            File.WriteAllText(
                Path.Combine(this.outputDirectory, "typeDescriptions.json"),
                JsonConvert.SerializeObject(this.typeDescriptions, Formatting.Indented));

        private void CreateUdtTypeInitializerClass()
        {
             var mappings = new List<string>();
            this.udtClasses.ForEach(c => mappings.Add($"                {c}.Map(session);"));
            var udtMapping = $@"        
        public static bool AlreadyMapped = false;
        public static void Map(Cassandra.Session session)
        {{

            if (!AlreadyMapped)
            {{
                session.Execute(""use {this.keySpaceName.ToLowerInvariant()};"");
{string.Join("\n", mappings)}
            }}
            AlreadyMapped = true;
        }}";
            this.CreateCSFile(
                $"UdtMapping",
                new List<string>(),
                "",
                "",
                udtMapping);
        }

        private void CreateCSFile(
            string className,
            List<string> properties,
            string optionalUsings = "",
            string optionalTableAttribute = "",
            string optionalUdtMapping = ""
            )
        {
            properties.Insert(0, "");
            var classDefinition = $@"
namespace {Utils.Utils.CSharpifyName(this.keySpaceName)}.Entities
{{
    using System;
    using System.Collections.Generic;
    {optionalUsings}
    {optionalTableAttribute}
    public class {className}
    {{
        {optionalUdtMapping}

        public {className}() {{ }}

        {string.Join("\n", properties)}
    }}
}}";
            var classFileName = Path.Combine(this.modelDirectory, className + ".cs");
            File.WriteAllText(classFileName, classDefinition);
            log.Add($"Created {classFileName}");
        }


        private string GetListTypeName(ColumnDescOrTableColumn tableColumn)
        {
            var typeInfo = ((Cassandra.ListColumnInfo)tableColumn.TypeInfo);
            
            var fakeTableColumn = new TableColumn
            {
                TypeCode = typeInfo.ValueTypeCode,
                TypeInfo = typeInfo,
                Keyspace = this.keySpaceName,
                Table = tableColumn.Table,
            };
            string memberType = null;
            if (typeInfo.ValueTypeCode == Cassandra.ColumnTypeCode.Udt)
            {
                fakeTableColumn.TypeInfo = typeInfo.ValueTypeInfo;
                this.isFrozen = true;
                memberType = this.GetCSharpTypeName(fakeTableColumn);
            }
            else
            {
                memberType = this.GetCSharpTypeName(fakeTableColumn);
            }

            return memberType != null ? $"List<{memberType}>" : null;
        }

        private string GetUDTTypeName(ColumnDescOrTableColumn tableColumn)
        {
            var typeInfo = ((Cassandra.UdtColumnInfo)tableColumn.TypeInfo);

            var typeName = typeInfo.Name;
            if (typeName.Contains("."))
            {
                typeName = typeName.Split(".").ToList().Last();
            }
            var className = Utils.Utils.CSharpifyName(typeName);
            var properties = new List<string>(){""};
            var udtMappings = new List<string>();
            foreach (var field in typeInfo.Fields)
            {
                var columnNameCS = Utils.Utils.CSharpifyName(field.Name);
                var csharpTypeName = GetCSharpTypeName(field);
                if (csharpTypeName != null)
                {
                    properties.Add($"        public {csharpTypeName} {columnNameCS} {{ get; set; }}");
                    udtMappings.Add($"                        .Map(a => a.{columnNameCS}, \"{field.Name}\")");
                }
            }

            var udtMapping = $@"        
        public static void Map(Cassandra.Session session)
        {{
            session
                .UserDefinedTypes
                .Define(
                    Cassandra
                    .UdtMap
                    .For<{className}>(""{typeName}"")
{string.Join("\n", udtMappings)}
            );
        }}";


            this.CreateCSFile(className, properties, "", "", udtMapping);
            this.udtClasses.Add(className);
            return className;
        }
        /*
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
         **/
        private static string GetSwaggerFormat(ColumnDescription columnDescription)
        {
            if (columnDescription.CSharpType.StartsWith("List"))
            {
                return null;
            }
            var cSharpType = columnDescription.CSharpType;
            return GetSwaggerFormat(cSharpType);
        }

        private static string GetSwaggerFormat(string cSharpType)
        {
            switch (cSharpType.ToLowerInvariant())
            {
                case "datetime":
                    return "date-time";
                case "long":
                    return "int64";
                case "int":
                    return "int32";
                case "short":
                    return "int16";
                case "byte":
                    return "int8";
            }
            return null;
        }

        private string GetSwaggerRef(ColumnDescription columnDescription)
        {
            if (columnDescription.CassandraType == "Udt")
            {
                this.AddSwaggerUdtIfNeeded(columnDescription);
                return columnDescription.IsUdt() ? $"#/definitions/{columnDescription.CSharpType}" : columnDescription.CSharpType;
            }

            return null;
        }

        private void AddSwaggerUdtIfNeeded(ColumnDescription columnDescription)
        {
            var udtName = (columnDescription.CassandraType.ToLowerInvariant() == "list") 
                ? columnDescription.CSharpType.Replace("List<", "").Replace(">", "") 
                : columnDescription.CSharpType;
            var udtNameCassandrified = Utils.Utils.CassandrifyName(udtName);
            var udtDef = this.session.Cluster.Metadata.GetUdtDefinition(this.keySpaceName.ToLowerInvariant(), udtNameCassandrified);
            if (udtDef != null && this.swaggerRoot.Definitions.Keys.All(k => k != udtName))
            {
                var schema = new Schema { Type = "object", Properties = new Dictionary<string, Schema>() };

                foreach (var field in udtDef.Fields)
                {
                    var csharpType = this.GetCSharpTypeName(field);
                    var propertySchema = new Schema
                    {
                        Type = GetSwaggerType(csharpType),
                        Format = GetSwaggerFormat(csharpType),
                    };
                    schema.Properties.Add(Utils.Utils.CamelCase(field.Name), propertySchema);
                }

                this.swaggerRoot.Definitions.Add(udtName, schema);
            }
        }

        private string GetSwaggerItemsRef(ColumnDescription columnDescription)
        {
            if (columnDescription.CassandraType.ToLowerInvariant() == "list" && columnDescription.IsUdt())
            {
                var r = columnDescription.CSharpType.Replace("List<", "").Replace(">", "");
                this.AddSwaggerUdtIfNeeded(columnDescription);
                return $"#/definitions/{r}";
            }
            return null;
        }

        private string GetSwaggerItemsType(ColumnDescription columnDescription)
        {
            if (columnDescription.CassandraType.ToLowerInvariant() == "list" && !columnDescription.IsUdt())
            {
                return columnDescription.CSharpType.Replace("List<", "").Replace(">", "");
            }
            return null;
        }

        private string GetSwaggerType(ColumnDescription columnDescription)
        {
            if (columnDescription.CassandraType.ToLowerInvariant() == "list")
            {
                return "array";
            }
            return GetSwaggerType(columnDescription.CSharpType);
        }

        private static string GetSwaggerType(string csharpType)
        {
            switch (csharpType.ToLowerInvariant())
            {
                case "bool":
                    return "boolean";
                case "datetime":
                    return "string";
                case "decimal":
                    return "number";
                case "double":
                    return "number";
                case "float":
                    return "number";
                case "guid":
                    return "string";
                case "int":
                    return "number";
                case "long":
                    return "number";
                case "string":
                    return "string";
            }

            return null;
        }
    }
}