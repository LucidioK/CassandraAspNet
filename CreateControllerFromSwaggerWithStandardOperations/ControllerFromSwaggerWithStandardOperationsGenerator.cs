using Newtonsoft.Json;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CreateControllerFromSwaggerWithStandardOperations
{
    internal class ControllerFromSwaggerWithStandardOperationsGenerator
    {
        class ReplacementParameters
        {
            public string NamespaceBase { get; set; }
            public string ApiVersion { get; set; }
            public string EntityName { get; set; }
            public string EntityNameCamelCase { get; set; }
            public string FilterParameters { get; set; }
            public string PrimaryKeyColumnType { get; set; }
            public string FilterParametersWithPrecedingComa { get; set; }
            public string PrimaryKeyColumnName { get; set; }
            public string OptionalRowFilteringCode { get; set; }
            public string ConnectionString { get; set; }
            public string MaximumNumberOfRows { get; set; }
            public string ProducesResponseAttributes { get; set; }
            public string HttpReturnCode { get; set; }
            public string FilteringFieldCamelCase { get; set; }
            public string FilteringField { get; set; }
            public string Quote { get; set; }

        }

        ReplacementParameters replacementParameters = new ReplacementParameters();
        string swaggerWithStandardOperationsJson;
        string connectionString;
        int apiVersion;
        int maxNumberOfRows;
        string csProjFile;
        string csProjDirectory;
        string controllersDirectory;
        string typeDescriptionsFile;
        List<Utils.TypeDescription> typeDescriptions;

        public ControllerFromSwaggerWithStandardOperationsGenerator(
            string swaggerWithStandardOperationsJson, 
            string connectionString, 
            int apiVersion, 
            int maxNumberOfRows, 
            string csProjFile,
            string typeDescriptionsFile)
        {
            this.swaggerWithStandardOperationsJson = Path.GetFullPath(swaggerWithStandardOperationsJson);
            if (!File.Exists(swaggerWithStandardOperationsJson))
            {
                throw new FileNotFoundException(swaggerWithStandardOperationsJson);
            }
            this.connectionString = connectionString;
            this.apiVersion = apiVersion;
            this.maxNumberOfRows = maxNumberOfRows;
            this.csProjFile = Path.GetFullPath(csProjFile);
            if (!File.Exists(this.csProjFile))
            {
                throw new FileNotFoundException(csProjFile);
            }
            this.csProjDirectory = Path.GetDirectoryName(this.csProjFile);
            this.controllersDirectory = Path.Combine(this.csProjDirectory, "Controllers");
            if (!Directory.Exists(this.controllersDirectory))
            {
                Directory.CreateDirectory(this.controllersDirectory);
            }
            this.typeDescriptions = JsonConvert.DeserializeObject<List<Utils.TypeDescription>>(File.ReadAllText(typeDescriptionsFile));
        }

        internal void Generate()
        {
            var swaggerInputText = Utils.Utils.PreprocessSwaggerFile(File.ReadAllText(this.swaggerWithStandardOperationsJson));
            var obj = new object();
            SwaggerRoot swaggerRoot;
            try
            {
                swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(swaggerInputText);
            }
            catch (JsonSerializationException)
            {
                if (Swagger.ObjectModel.SimpleJson.TryDeserializeObject(swaggerInputText, out obj))
                {
                    var jso = (JsonObject)obj;
                    var security = (JsonArray)jso["security"];
                    jso.Remove("security");
                    var ss = JsonConvert.SerializeObject(jso);
                    swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(ss);
                    ReinjectSecurity(swaggerRoot, security);
                }
                else
                {
                    throw new Exception($"Could not deserialize {this.swaggerWithStandardOperationsJson}");
                }
            }
            //var swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(swaggerInputText);
            var controllerNameSpace = Utils.Utils.CSharpifyName(Path.GetFileNameWithoutExtension(this.csProjFile));
            replacementParameters.ApiVersion = this.apiVersion.ToString();
            replacementParameters.MaximumNumberOfRows = this.maxNumberOfRows.ToString();
            replacementParameters.NamespaceBase = controllerNameSpace;
            replacementParameters.ConnectionString = this.connectionString;
            foreach (var path in swaggerRoot.Paths)
            {
                CreateController(path.Key, path.Value);
            }
            CreateCSFile(this.csProjDirectory, "Constants.cs", Constants.ConstantsCode);
            CreateCSFile(this.csProjDirectory, "AppSettings.cs", Constants.AppSettingsCode);
        }

        private void ReinjectSecurity(SwaggerRoot swaggerRoot, JsonArray security)
        {
            swaggerRoot.Security = new Dictionary<SecuritySchemes, IEnumerable<string>>();
            foreach (JsonObject sec in security)
            {
                foreach (var securitySchemeName in sec.Keys)
                {
                    var securityScheme = (SecuritySchemes)Enum.Parse(typeof(SecuritySchemes), securitySchemeName);
                    var permissions = (JsonArray)sec[securitySchemeName];
                    var permissionsList = new List<string>();
                    permissions.ForEach(p => permissionsList.Add((string)p));
                    swaggerRoot.Security.Add(securityScheme, permissionsList);
                }
            }
        }

        private void CreateController(string key, PathItem pathItem)
        {
            replacementParameters.EntityName = key.Split('/')[1];
            replacementParameters.EntityNameCamelCase = Utils.Utils.CamelCase(replacementParameters.EntityName);
            replacementParameters.ProducesResponseAttributes = CreateProducesResponseAttributes(pathItem);
            PopulatePrimaryKeyNameAndType(pathItem);
            PopulateFilterParameters(pathItem);
            CreateCSFile(this.controllersDirectory, replacementParameters.EntityName + "Controller.cs", Constants.ControllerCode);
        }

        private void CreateCSFile(string directory, string fileName, string code)
        {
            var constantsFilePath = Path.Combine(directory, fileName);
            File.WriteAllText(constantsFilePath, RunReplacements(code, replacementParameters));
        }

        private void PopulateFilterParameters(PathItem pathItem)
        {
            var queryParameters = pathItem.Get.Parameters.ToList().Where(p => p.In == ParameterIn.Query).ToList();
            replacementParameters.FilterParameters = "";
            replacementParameters.OptionalRowFilteringCode = "";
            var filterParameters = new StringBuilder();
            var rowFilteringCode = new StringBuilder();
            foreach (var queryParameter in queryParameters)
            {
                if (filterParameters.Length > 0)
                {
                    filterParameters.Append(", ");
                }
                filterParameters.Append(GetParameterTypeFromParameter(queryParameter));
                filterParameters.Append("? ");
                filterParameters.Append(queryParameter.Name);
                filterParameters.Append(" = null");

                rowFilteringCode.AppendLine(CreateRowFilteringCodeLine(queryParameter));
            }
            replacementParameters.FilterParameters = filterParameters.ToString();
            replacementParameters.OptionalRowFilteringCode = rowFilteringCode.ToString();
            if (replacementParameters.FilterParameters.Length > 0)
            {
                replacementParameters.FilterParametersWithPrecedingComa = ", " + replacementParameters.FilterParameters;
            }
        }

        private string CreateRowFilteringCodeLine(Parameter queryParameter)
        {
            replacementParameters.FilteringField = Utils.Utils.CSharpifyName(queryParameter.Name);
            replacementParameters.FilteringFieldCamelCase = Utils.Utils.CamelCase(queryParameter.Name);
            replacementParameters.Quote = "";
            var type = GetParameterTypeFromParameter(queryParameter);
            if (type == "string" || type == "DateTime")
            {
                replacementParameters.Quote = "'";
            }
            
            return RunReplacements(Constants.RowFilteringCode, replacementParameters);
        }

        //if (^FilteringFieldCamelCase^ != null) rows = rows.Where(r => r.^FilteringField^ == ^Quote^^FilteringFieldCamelCase^^Quote^).ToList();

        private void PopulatePrimaryKeyNameAndType(PathItem pathItem)
        {
            var pathParameters = pathItem.Get.Parameters.ToList().Where(p => p.In == ParameterIn.Path).ToList();
            var primaryColumnDescription = typeDescriptions
                    .First(t => t.CSharpName == replacementParameters.EntityName)
                    ?.ColumnDescriptions
                    ?.First(c => c.IsPartitionKey);
            if (pathParameters != null && pathParameters.Any() && primaryColumnDescription != null)
            {
                replacementParameters.PrimaryKeyColumnName = primaryColumnDescription.CSharpName;
                replacementParameters.PrimaryKeyColumnType = primaryColumnDescription.CSharpType;
            }

        }

        private string CreateProducesResponseAttributes(PathItem pathItem)
        {
            var producesResponseAttributes = new StringBuilder();
            foreach (var response in pathItem.Get.Responses)
            {
                replacementParameters.HttpReturnCode = response.Key;

                var producesResponseAttribute = RunReplacements(Constants.ProducesResponseType, replacementParameters);
                producesResponseAttributes.AppendLine(producesResponseAttribute);
            }
            return producesResponseAttributes.ToString();
        }

        private static string RunReplacements(string str, ReplacementParameters replacementParameters)
        {
            foreach (var property in replacementParameters.GetType().GetTypeInfo().GetProperties())
            {
                var slug = "^"+property.Name+"^";
                var value = (string)(property.GetValue(replacementParameters) ?? "");
                str = str.Replace(slug, value);
            }
            return str;
        }

        private static string GetParameterTypeFromParameter(Parameter parameter)
        {
            if (parameter.Format == null && parameter.Type == "string")
            {
                return "string";
            }
            switch (parameter.Format.ToLowerInvariant())
            {
                case "int64":
                    return "long";
                case "int32":
                    return "int";
                case "int16":
                    return "short";
                case "int8":
                    return "byte";
                case "date-time":
                    return "DateTime";
                default:
                    return null;
            }
        }
    }
}