

namespace GenerateSwaggerStandardOperations
{
    using Newtonsoft.Json;
    using Swagger.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal class SwaggerStandardOperationsGenerator
    {
        Dictionary<string, SecurityScheme> generalSecurityScheme;
        Dictionary<string, Response> generalResponses;
        string swaggerFromNSwagJson;
        string typeDescriptionsJson;
        string outputSwaggerJson;
        string oauthUrl;
        string keyNameSpace;

        public SwaggerStandardOperationsGenerator(string keyNameSpace, string swaggerFromNSwagJson, string typeDescriptionsJson, string outputSwaggerJson, string oauthUrl)
        {
            this.keyNameSpace = keyNameSpace;
            this.swaggerFromNSwagJson = swaggerFromNSwagJson;
            this.typeDescriptionsJson = typeDescriptionsJson;
            this.outputSwaggerJson = outputSwaggerJson;
            this.oauthUrl = oauthUrl;
        }

        internal void Generate()
        {
            generalResponses = CreateGeneralResponses();
            generalSecurityScheme = CreateSecurityDefinitions();
            var swaggerInputText = Utils.Utils.PreprocessSwaggerFile(File.ReadAllText(this.swaggerFromNSwagJson));
            var swaggerRoot = JsonConvert.DeserializeObject<SwaggerRoot>(swaggerInputText);
            var td = JsonConvert.DeserializeObject<List<Utils.TypeDescription>>(File.ReadAllText(this.typeDescriptionsJson));
            swaggerRoot.SecurityDefinitions = generalSecurityScheme;
            CreateTags(swaggerRoot);
            swaggerRoot.Security = new Dictionary<SecuritySchemes, IEnumerable<string>>
            {
                { SecuritySchemes.Oauth2, new List<string>{ "read", "write" } }
            };
            foreach (var typeDescription in td)
            {

                var pathItem = CreatePathItem(typeDescription);
                swaggerRoot.Paths.Add("/" + typeDescription.CSharpName, pathItem);
                pathItem = CreatePathItem(typeDescription, "{id}");
                swaggerRoot.Paths.Add("/" + typeDescription.CSharpName + "/{id}", pathItem);
            }
            File.WriteAllText(this.outputSwaggerJson, swaggerRoot.ToJson());
        }

        private void CreateTags(SwaggerRoot swaggerRoot)
        {
            if (swaggerRoot.Tags == null)
            {
                swaggerRoot.Tags = new List<Tag>();
            }
            var tags = swaggerRoot.Tags.ToList();
            if (tags.All(t => t.Name != "Namespace"))
            {
                tags.Add(new Tag { Name = "Namespace", Description = Utils.Utils.CSharpifyName(this.keyNameSpace) });
            }
            swaggerRoot.Tags = tags;
        }

        private static Dictionary<string, Response> CreateGeneralResponses()
        {
            var responses = new Dictionary<string, Response>();
            responses.Add("200", new Response
            {
                Description = "Successful",
                Schema = new Schema { Ref = "#/definitions/TYPENAME" }
            });
            responses.Add("201", new Response
            {
                Description = "Added new TYPENAME",
                Schema = new Schema { Ref = "#/definitions/TYPENAME" }
            });
            responses.Add("303", new Response
            {
                Description = "See other",
            });
            responses.Add("400", new Response
            {
                Description = "Invalid TYPENAME",
            });
            responses.Add("401", new Response
            {
                Description = "Not authorized",
            });
            responses.Add("404", new Response
            {
                Description = "TYPENAME not found",
            });
            responses.Add("500", new Response
            {
                Description = "Internal error",
            });
            return responses;
        }

        private Dictionary<string, Response> CreateResponsesFor(List<string> httpResultCodes, string typeName)
        {
            var responses = new Dictionary<string, Response>();
            httpResultCodes.ForEach(c => responses.Add(c, InjectTypeNameIntoResponse(generalResponses[c], typeName)));
            return responses;
        }

        private static Response InjectTypeNameIntoResponse(Response response, string typeName)
        {
            response.Description = response.Description.Replace("TYPENAME", typeName);
            if (response.Schema != null && response.Schema.Ref != null)
            {
                response.Schema.Ref = response.Schema.Ref.Replace("TYPENAME", typeName);
            }
            return response;
        }

        private  Dictionary<string, SecurityScheme> CreateSecurityDefinitions()
        {
            var secDefs = new Dictionary<string, SecurityScheme>();
            var securityScheme = new SecurityScheme()
            {
                Type = SecuritySchemes.Oauth2,
                AuthorizationUrl = oauthUrl,
                Flow = Oauth2Flows.Implicit,
                Scopes = new Dictionary<string, string>
                {
                    { "read", "Read privilege." },
                    { "write", "Write privilege." },
                }
            };
            secDefs.Add("Oauth2", securityScheme);
            return secDefs;
        }

         List<string> acceptedFormats = new List<string>() { "application/json" };

        private  PathItem CreatePathItem(Utils.TypeDescription typeDescription, string idParam = null)
        {
            var pathItem = new PathItem
            {
                Parameters = null
            };

            var readPrivilege = (new List<string> { "read" }).AsEnumerable();
            var readSecurityRequirement = new Dictionary<SecuritySchemes, IEnumerable<string>>
                {{ SecuritySchemes.Oauth2, readPrivilege }};
            var readWritePrivilege = (new List<string> { "read", "write" }).AsEnumerable();
            var readWriteSecurityRequirement = new Dictionary<SecuritySchemes, IEnumerable<string>>
                {{ SecuritySchemes.Oauth2, readWritePrivilege }};

            var getParameter = new Parameter
            {
                In = ParameterIn.Body,
                Required = true,
                Ref = $"#/definitions/{typeDescription.CSharpName}"
            };
            var getParameters = new List<Parameter> { getParameter };
            pathItem.Get = new Operation
            {
                Produces = acceptedFormats,
                Consumes = acceptedFormats,
                Description = $"Retrieves all {typeDescription.CSharpName}s.",
                OperationId = $"get{typeDescription.CSharpName}" + ((idParam != null) ? "ById" : ""),
                SecurityRequirements = (IDictionary<SecuritySchemes, IEnumerable<string>>)readSecurityRequirement,
                Parameters = CreateGetParameters(typeDescription, idParam != null),
                Responses = CreateResponsesFor(new List<string> { "200", "303", "401", "404", "500" }, typeDescription.CSharpName)
            };

            /*
                        pathItem.Post = new Operation
                        {
                            Produces = acceptedFormats,
                            Consumes = acceptedFormats,
                            Description = $"Creates a {typeDescription.Name}.",
                            OperationId = $"add{typeDescription.Name}",
                            SecurityRequirements = (IDictionary<SecuritySchemes, IEnumerable<string>>)readWriteSecurityRequirement,
                            Parameters = new List<Parameter> {
                                new Parameter
                                {
                                    In = ParameterIn.Body,
                                    Required = true,
                                    Ref = $"#/definitions/{typeDescription.Name}"
                                } },
                            Responses = CreateResponsesFor(new List<string> { "200", "201", "303", "401", "404", "500" }, typeDescription.Name)
                        };

                        if (idParam == null)
                        {
                            pathItem.Put = new Operation
                            {
                                Produces = acceptedFormats,
                                Consumes = acceptedFormats,
                                Description = $"Update an existing {typeDescription.Name}.",
                                OperationId = $"update{typeDescription.Name}",
                                Responses = pathItem.Get.Responses,
                            };
                        }
                        else
                        {
                            pathItem.Delete = pathItem.Post;
                            pathItem.Delete.Description = $"Delete an existing {typeDescription.Name}.";
                            pathItem.Delete.OperationId = $"delete{typeDescription.Name}";
                        }
            */
            return pathItem;
        }

        private  List<Parameter> CreateGetParameters(Utils.TypeDescription typeDescription, bool needPathParameter)
        {
            var parameters = new List<Parameter>();
            var partitionKey = typeDescription.ColumnDescriptions.FirstOrDefault(c => c.IsPartitionKey);
            if (needPathParameter && partitionKey != default(Utils.ColumnDescription))
            {

                parameters.Add(
                    new Parameter
                    {
                        Name = "id",
                        In = ParameterIn.Path,
                        Required = true,
                        Type = "string",
                        Description = $"{partitionKey.CamelCaseName}: Id for {typeDescription.CSharpName}",
                    }
                );
            }
            var indexes = typeDescription.ColumnDescriptions.Where(c => c.IsClusteringKey || c.IsIndex).ToList();
            foreach (var index in indexes)
            {
                var parameter = new Parameter
                {
                    Name = Utils.Utils.CamelCase(index.CamelCaseName),
                    In = ParameterIn.Query,
                    Required = false,
                    Type = GetParameterType(index.CassandraType),
                    Format = GetParameterFormat(index.CassandraType)
                };
                parameters.Add(parameter);
            }
            return parameters;
        }

        private static string GetParameterFormat(string type)
        {
            switch (type.ToLowerInvariant())
            {

                case "bigint":
                    return "int64";
                case "int":
                    return "int32";
                case "smallint":
                    return "int16";
                case "tinyint":
                    return "int8";
                case "timestamp":
                    return "date-time";
                default:
                    return null;
            }
        }

        private static string GetParameterType(string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "boolean":
                    return "boolean";
                case "bigint":
                case "int":
                case "smallint":
                case "tinyint":
                    return "integer";
                case "text":
                case "ascii":
                case "timestamp":
                    return "string";
                default:
                    return null;
            }
        }


    }
}
