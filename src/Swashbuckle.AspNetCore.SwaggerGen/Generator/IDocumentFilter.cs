using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public interface IDocumentFilter
    {
        void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context);
    }

    public class DocumentFilterContext
    {
        public DocumentFilterContext(
            ApiDescriptionGroupCollection apiDescriptionsGroups,
            ISchemaProvider schemaProvider,
            IDictionary<string, Schema> definitions)
        {
            ApiDescriptionsGroups = apiDescriptionsGroups;
            SchemaProvider = schemaProvider;
            Definitions = definitions;

            // For backwards compatability until the next major release
            SchemaRegistry = new SchemaRegistry(schemaProvider, definitions);
        }

        public ApiDescriptionGroupCollection ApiDescriptionsGroups { get; private set; }

        public ISchemaProvider SchemaProvider { get; private set; }

        public IDictionary<string, Schema> Definitions { get; private set; }

        [Obsolete("TODO:", false)]
        public ISchemaRegistry SchemaRegistry { get; private set; }
    }
}
