using Cassandra;
using Newtonsoft.Json;
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
        private List<string> udtClasses = new List<string>();
        private string csproj = @"
<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <AssemblyName>App</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include='CassandraCSharpDriver'                                 Version='3.4.0.1' />
    <PackageReference Include='Microsoft.AspNet.WebApi'                               Version='5.2.3'   />
	<PackageReference Include='Microsoft.AspNet.WebApi.Owin'                          Version='5.2.3'   />
    <PackageReference Include='Microsoft.AspNetCore.All'                              Version='2.0.3'   />
    <PackageReference Include='Microsoft.Extensions.DependencyInjection.Abstractions' Version='2.0.0'   />
    <PackageReference Include='Microsoft.Owin.Cors'                                   Version='3.1.0'   />
    <PackageReference Include='Microsoft.Owin.Host.SystemWeb'                         Version='3.1.0'   />
    <PackageReference Include='Microsoft.Owin.Security.OAuth'                         Version='3.1.0'   />
    <PackageReference Include='Microsoft.VisualStudio.Web.CodeGeneration.Design'      Version='2.0.1'   />
    <PackageReference Include='Newtonsoft.Json'                                       Version='10.0.1'  />
    <PackageReference Include='Ninject'                                               Version='3.3.4'   />
    <PackageReference Include='Swashbuckle.AspNetCore'                                Version='1.1.0'   />
    <PackageReference Include='System.IdentityModel.Tokens.Jwt'                       Version='5.1.5'   />
    <PackageReference Include='Thinktecture.IdentityModel.Core'                       Version='1.4.0'   />
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
            
            this.keySpaceName = keySpaceName;
            this.outputDirectory = outputDirectory;
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
                    $@"[Table(""{tableDef.Name}"", AllowFiltering = true)]");
                this.typeDescriptions.Add(
                    new Utils.TypeDescription
                    {
                        CassandraTableName = tableDef.Name,
                        CSharpName = className,
                        ColumnDescriptions = columnDescriptions
                    });
            }
            this.CreateUdtTypeInitializerClass();
            this.CreateTypeDescriptionJson();
            File.WriteAllText(Path.Combine(this.outputDirectory, this.keySpaceName + ".csproj"), this.csproj);
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
                CamelCaseName = Utils.Utils.CamelCase(columnName),
                CSharpName = columnNameCS,
                IsPartitionKey = tableDef.PartitionKeys.Any(k => k.Name == columnName),
                IsClusteringKey = tableDef.ClusteringKeys.Any(k => k.Item1.Name == columnName),
                CassandraType = tableColumn.TypeCode.ToString(),
                CSharpType = csharpTypeName,
                IsFrozen = this.isFrozen
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
            var classFileName = Path.Combine(this.outputDirectory, className + ".cs");
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
    }
}