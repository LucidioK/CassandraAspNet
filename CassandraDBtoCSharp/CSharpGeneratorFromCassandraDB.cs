using Cassandra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CassandraDBtoCSharp
{

    internal class CSharpGeneratorFromCassandraDB
    {
        private List<Utils.TypeDescription> typeDescriptions = new List<Utils.TypeDescription>();
        private List<string> log = new List<string>();
        public List<string> Log => log;
        private Session session;
        private string keySpaceName;
        private string outputDirectory;
        private List<string> udtClasses = new List<string>();
        private string csproj = @"
<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include='CassandraCSharpDriver' Version='3.4.0.1' />
    <PackageReference Include='Newtonsoft.Json' Version='10.0.1' />
    <PackageReference Include='Microsoft.AspNetCore.All' Version='2.0.3' />
    <PackageReference Include='Microsoft.Extensions.DependencyInjection.Abstractions' Version='2.0.0' />
    <PackageReference Include = 'Microsoft.VisualStudio.Web.CodeGeneration.Design' Version='2.0.1' />
  </ItemGroup>
</Project>
";

        private Dictionary<Cassandra.ColumnTypeCode, Func<TableColumn,string>> cassandraToCSharpTypeEquivalency;
        public CSharpGeneratorFromCassandraDB(string connectionString, string keySpaceName, string outputDirectory)
        {
            this.session = (Session)Cluster.Builder().WithConnectionString(connectionString).Build().Connect();
            
            this.keySpaceName = keySpaceName;
            this.outputDirectory = outputDirectory;
            this.cassandraToCSharpTypeEquivalency = new Dictionary<ColumnTypeCode, Func<TableColumn, string>>()
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
                { Cassandra.ColumnTypeCode.List      , t => "List" },
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
            var tables = session.Cluster.Metadata.GetTables(this.keySpaceName).ToList();
            log.Add($"Starting generation, {tables.Count} tables.");
            foreach (var tableName in tables)
            {
                var tableDef = session.Cluster.Metadata.GetTable(this.keySpaceName, tableName);
                var columnDescriptions = new List<Utils.ColumnDescription>();
                var className = Utils.Utils.CSharpifyName(tableDef.Name);
                log.Add($" Starting table {tableDef.Name}, class {className}");
                var properties = new List<string> { "" };
                foreach (var tableColumn in tableDef.TableColumns)
                {
                    log.Add($" Table {tableDef.Name}, class {className}, column {tableColumn.Name}");
                    Func<TableColumn, string> csharpTypeNameFunc;
                    if (this.cassandraToCSharpTypeEquivalency.TryGetValue(tableColumn.TypeCode, out csharpTypeNameFunc))
                    {
                        var csharpTypeName = csharpTypeNameFunc(tableColumn);
                        AddProperty(tableDef, columnDescriptions, properties, tableColumn, csharpTypeName);
                    }
                    else
                    {
                        log.Add($" Table {tableDef.Name}, class {className}, column {tableColumn.Name}: DON'T KNOW HOW TO HANDLE TYPE {tableColumn.TypeCode}");
                    }
                }
                this.CreateCSFile(
                    className,
                    properties,
                    "using Cassandra.Mapping.Attributes;",
                    $@"[Table(""{tableDef.Name}"", AllowFiltering = true)]");
                this.typeDescriptions.Add(
                    new Utils.TypeDescription
                    {
                        CassandraTableName = tableDef.Name,
                        CSharpName = className,
                        ColumnDescriptions = columnDescriptions
                    });
            }
            this.CreateUdtTypeInitializerClassIfNeeded();
            this.CreateTypeDescriptionJson();
            File.WriteAllText(Path.Combine(this.outputDirectory, this.keySpaceName + ".csproj"), this.csproj);
        }

        private static void AddProperty(
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
                CamelCaseName = Utils.Utils.CamelCase(columnName),
                CSharpName = columnNameCS,
                IsPartitionKey = tableDef.PartitionKeys.Any(k => k.Name == columnName),
                IsClusteringKey = tableDef.ClusteringKeys.Any(k => k.Item1.Name == columnName),
                CassandraType = tableColumn.TypeCode.ToString(),
                CSharpType = csharpTypeName
            };
            columnDescriptions.Add(columnDescription);
            if (columnDescription.IsPartitionKey)
            {
                properties.Add("        [PartitionKey]");
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

        private void CreateUdtTypeInitializerClassIfNeeded()
        {
            if (this.udtClasses.Any())
            {
                var mappings = new List<string>();
                this.udtClasses.ForEach(c => mappings.Add($"            {c}.Map(session);"));
                var udtMapping = $@"        
        public static void Map(Cassandra.Session session)
        {{
{string.Join("\n", mappings)}
        }}";
                this.CreateCSFile(
                    $"{this.keySpaceName}UdtMapping",
                    new List<string>(),
                    "",
                    "",
                    udtMapping);
            }
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
    {optionalUsings}
    {optionalTableAttribute}
    public class {className}
    {{
        {optionalUdtMapping}

        public {className}() {{ }}

        {string.Join("\n", properties)}
    }}
}}";
            var classFileName = Path.Combine(this.outputDirectory, className + ".cs");
            File.WriteAllText(classFileName, classDefinition);
            log.Add($"Created {classFileName}");
        }

        private string GetUDTTypeName(TableColumn tableColumn)
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
                Func<TableColumn, string> csharpTypeNameFunc;
                if (this.cassandraToCSharpTypeEquivalency.TryGetValue(field.TypeCode, out csharpTypeNameFunc))
                {
                    var csharpTypeName = csharpTypeNameFunc(tableColumn);
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
    }
}