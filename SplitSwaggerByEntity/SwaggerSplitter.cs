
namespace SplitSwaggerByEntity
{
    using Newtonsoft.Json;
    using Pluralize.NET;
    using Swagger.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    class SwaggerSplitter
    {
        private string swaggerJsonFile;
        private string outputFolder;
        private Pluralizer pluralizer = new Pluralizer();
        public SwaggerSplitter(string swaggerJsonFile, string outputFolder)
        {
            this.swaggerJsonFile = swaggerJsonFile;
            this.outputFolder = outputFolder;
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
        }

        public void Generate()
        {
            var swaggerRoot = Utils.Utils.LoadSwagger(this.swaggerJsonFile);
            foreach (var entity in swaggerRoot.Definitions)
            {
                var entityName = entity.Key;
                var swaggerCopy = this.DeleteAllPathsNotRelatedTo(swaggerRoot, entityName);
                if (swaggerCopy != null)
                {
                    this.SaveSplitterSwagger(swaggerCopy, entityName);
                }
            }
        }

        private void SaveSplitterSwagger(SwaggerRoot swagger, string entityName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.swaggerJsonFile);
            var extension = Path.GetExtension(this.swaggerJsonFile);
            var newFileName = fileNameWithoutExtension + entityName + "." + extension;
            newFileName = Path.Combine(this.outputFolder, newFileName);
            File.WriteAllText(newFileName, swagger.ToJson());
        }

        private SwaggerRoot DeleteAllPathsNotRelatedTo(SwaggerRoot swaggerRoot, string entityName)
        {
            var entityNamePlural = pluralizer.Pluralize(entityName);
            var prefix = $"/{entityNamePlural.ToLowerInvariant()}/";
            var swaggerCopy = Utils.Utils.Clone(swaggerRoot);
            swaggerRoot
                .Paths
                .Keys
                .Where(p => !Utils.Utils.InvariantStartsWith(p, prefix))
                .ToList()
                .ForEach(p => swaggerCopy.Paths.Remove(p));

            return swaggerCopy.Paths.Any() ? swaggerCopy : null;

        }
    }
}
