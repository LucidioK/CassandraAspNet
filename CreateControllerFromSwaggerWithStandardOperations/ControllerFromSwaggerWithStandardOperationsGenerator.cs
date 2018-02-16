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
            public string FilterParametersNamesOnlyWithPrecedingComa { get; set; }
            public string PrimaryKeyColumnName { get; set; }
            public string OptionalRowFilteringCode { get; set; }
            public string connectionStringOrCassandraSettingsJsonContent { get; set; }
            public string MaximumNumberOfRows { get; set; }
            public string ProducesResponseAttributes { get; set; }
            public string HttpReturnCode { get; set; }
            public string FilteringFieldCamelCase { get; set; }
            public string FilteringField { get; set; }
            public string Quote { get; set; }
        }

        ReplacementParameters _replacementParameters = new ReplacementParameters();
        string _swaggerWithStandardOperationsJson;
        string _connectionString;
        int _apiVersion;
        int _maxNumberOfRows;
        string _csProjFile;
        string _csProjDirectory;
        string _controllersDirectory;
        string _typeDescriptionsFile;
        List<Utils.TypeDescription> _typeDescriptions;

        public ControllerFromSwaggerWithStandardOperationsGenerator(
            string swaggerWithStandardOperationsJson,
            string connectionString,
            int apiVersion,
            int maxNumberOfRows,
            string csProjFile,
            string typeDescriptionsFile)
        {
            this._swaggerWithStandardOperationsJson = Path.GetFullPath(swaggerWithStandardOperationsJson);
            if (!File.Exists(swaggerWithStandardOperationsJson))
            {
                throw new FileNotFoundException(swaggerWithStandardOperationsJson);
            }
            this._connectionString = connectionString;
            this._apiVersion = apiVersion;
            this._maxNumberOfRows = maxNumberOfRows;
            this._csProjFile = Path.GetFullPath(csProjFile);
            if (!File.Exists(this._csProjFile))
            {
                throw new FileNotFoundException(csProjFile);
            }
            this._csProjDirectory = Path.GetDirectoryName(this._csProjFile);
            this._controllersDirectory = Path.Combine(this._csProjDirectory, "Controllers");
            if (!Directory.Exists(this._controllersDirectory))
            {
                Directory.CreateDirectory(this._controllersDirectory);
            }
            this._typeDescriptions = JsonConvert.DeserializeObject<List<Utils.TypeDescription>>(File.ReadAllText(typeDescriptionsFile));
        }

        internal void Generate()
        {
            var swaggerRoot = Utils.Utils.LoadSwagger(this._swaggerWithStandardOperationsJson);
            var controllerNameSpace = Utils.Utils.CSharpifyName(Path.GetFileNameWithoutExtension(this._csProjFile));
            _replacementParameters.ApiVersion = this._apiVersion.ToString();
            _replacementParameters.MaximumNumberOfRows = this._maxNumberOfRows.ToString();
            _replacementParameters.NamespaceBase = controllerNameSpace;
            SetConnectionStringOrCassandraSettingsJsonContent();
            foreach (var path in swaggerRoot.Paths)
            {
                CreateController(path.Key, path.Value);
            }
            CreateCSFile(this._csProjDirectory, "Constants.cs", Constants.ConstantsCode);
            CreateCSFile(this._csProjDirectory, "AppSettings.cs", Constants.AppSettingsCode);
            Utils.Utils.ReplaceInFiles(this._csProjDirectory, "*.cs", "namespace WebApp", $"namespace {_replacementParameters.NamespaceBase}");
            Utils.Utils.ReplaceInFiles(this._csProjDirectory, "*.cs", "using WebApp", $"using {_replacementParameters.NamespaceBase}");
        }

        private void SetConnectionStringOrCassandraSettingsJsonContent()
        {
            if ((Utils.Utils.InvariantContains(_connectionString, "Contact Points") &&
                _connectionString.Contains("=")))
            {
                _replacementParameters.connectionStringOrCassandraSettingsJsonContent = _connectionString;
            }
            else
            {
                var localConfiguration = JsonConvert.DeserializeObject<Utils.LocalSettings>(File.ReadAllText(_connectionString));
                _replacementParameters.connectionStringOrCassandraSettingsJsonContent = JsonConvert.SerializeObject(localConfiguration.CassandraSettings);
                _replacementParameters.connectionStringOrCassandraSettingsJsonContent = _replacementParameters.connectionStringOrCassandraSettingsJsonContent.Replace("\"", "'");
            }
        }

        private void CreateController(string key, PathItem pathItem)
        {
            _replacementParameters.EntityName = key.Split('/')[1];
            _replacementParameters.EntityNameCamelCase = Utils.Utils.CamelCase(_replacementParameters.EntityName);
            _replacementParameters.ProducesResponseAttributes = CreateProducesResponseAttributes(pathItem);
            PopulatePrimaryKeyNameAndType(pathItem);
            PopulateFilterParameters(pathItem);
            CreateCSFile(this._controllersDirectory, _replacementParameters.EntityName + "Controller.cs", Constants.ControllerCode);
        }

        private void CreateCSFile(string directory, string fileName, string code)
        {
            var constantsFilePath = Path.Combine(directory, fileName);
            File.WriteAllText(constantsFilePath, RunReplacements(code, _replacementParameters));
        }

        private void PopulateFilterParameters(PathItem pathItem)
        {
            var queryParameters = pathItem.Get.Parameters.ToList().Where(p => p.In == ParameterIn.Query).ToList();
            _replacementParameters.FilterParameters = "";
            _replacementParameters.OptionalRowFilteringCode = "";
            var filterParameters = new StringBuilder();
            var filterParametersNamesOnly = new StringBuilder();
            var rowFilteringCode = new StringBuilder();

            foreach (var queryParameter in queryParameters)
            {
                string type = GetParameterTypeFromParameter(queryParameter);

                if (type == null)
                {
                    continue;
                }
                if (filterParameters.Length > 0)
                {
                    filterParameters.Append(", ");
                    filterParametersNamesOnly.Append(", ");
                }

                filterParameters.Append(type);
                filterParameters.Append(" ");
                filterParameters.Append(queryParameter.Name);
                filterParameters.Append(" = null");

                filterParametersNamesOnly.Append(queryParameter.Name);
                rowFilteringCode.AppendLine(CreateRowFilteringCodeLine(queryParameter));
            }

            _replacementParameters.FilterParametersNamesOnlyWithPrecedingComa = filterParametersNamesOnly.ToString();
            _replacementParameters.FilterParameters = filterParameters.ToString();
            _replacementParameters.OptionalRowFilteringCode = rowFilteringCode.ToString();
            if (_replacementParameters.FilterParameters.Length > 0)
            {
                _replacementParameters.FilterParametersWithPrecedingComa = ", " + _replacementParameters.FilterParameters;
                _replacementParameters.FilterParametersNamesOnlyWithPrecedingComa = "," + _replacementParameters.FilterParametersNamesOnlyWithPrecedingComa;
            }
        }

        private string CreateRowFilteringCodeLine(Parameter queryParameter)
        {
            _replacementParameters.FilteringField = Utils.Utils.CSharpifyName(queryParameter.Name);
            _replacementParameters.FilteringFieldCamelCase = Utils.Utils.CamelCase(queryParameter.Name);
            _replacementParameters.Quote = "";

            var type = GetParameterTypeFromParameter(queryParameter);

            if (type == "string" || type == "DateTime")
            {
                _replacementParameters.Quote = "'";
            }

            return RunReplacements(Constants.RowFilteringCode, _replacementParameters);
        }

        //if (^FilteringFieldCamelCase^ != null) rows = rows.Where(r => r.^FilteringField^ == ^Quote^^FilteringFieldCamelCase^^Quote^).ToList();

        private void PopulatePrimaryKeyNameAndType(PathItem pathItem)
        {
            var pathParameters = pathItem.Get.Parameters.ToList().Where(p => p.In == ParameterIn.Path).ToList();
            var primaryColumnDescription = _typeDescriptions
                    .First(t => t.CSharpName == _replacementParameters.EntityName)
                    ?.ColumnDescriptions
                    ?.First(c => c.IsPartitionKey);

            if (pathParameters != null && pathParameters.Any() && primaryColumnDescription != null)
            {
                _replacementParameters.PrimaryKeyColumnName = primaryColumnDescription.CSharpName;
                _replacementParameters.PrimaryKeyColumnType = primaryColumnDescription.CSharpType;
            }
        }

        private string CreateProducesResponseAttributes(PathItem pathItem)
        {
            var producesResponseAttributes = new StringBuilder();

            foreach (var response in pathItem.Get.Responses)
            {
                _replacementParameters.HttpReturnCode = response.Key;

                var producesResponseAttribute = RunReplacements(Constants.ProducesResponseType, _replacementParameters);
                producesResponseAttributes.AppendLine(producesResponseAttribute);
            }
            return producesResponseAttributes.ToString();
        }

        private static string RunReplacements(string str, ReplacementParameters replacementParameters)
        {
            foreach (var property in replacementParameters.GetType().GetTypeInfo().GetProperties())
            {
                var slug = "^" + property.Name + "^";
                var value = (string)(property.GetValue(replacementParameters) ?? "");
                str = str.Replace(slug, value);
            }
            return str;
        }

        private static string GetParameterTypeFromParameter(Parameter parameter)
        {
            if ((parameter.Format == null && parameter.Type == "string")
                ||
                (parameter.Format == null && parameter.Type == null))
            {
                return "string";
            }
            switch (parameter.Type)
            {
                case "boolean":
                    return "bool?";
            }
            if (parameter.Format == null)
            {
                return null;
            }
            switch (parameter.Format.ToLowerInvariant())
            {
                case "int64":
                    return "long?";
                case "int32":
                    return "int?";
                case "int16":
                    return "short?";
                case "int8":
                    return "byte";
                case "date-time":
                    return "DateTime?";
                default:
                    return null;
            }
        }
    }
}