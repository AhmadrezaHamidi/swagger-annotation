using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public interface IOperationFilter
    {
        void Apply(Operation operation, OperationFilterContext context);
    }

    public class OperationFilterContext
    {
        public OperationFilterContext(
            ApiDescription apiDescription,
            ISchemaProvider schemaProvider,
            IDictionary<string, Schema> definitions)
        {
            ApiDescription = apiDescription;
            SchemaProvider = schemaProvider;
            Definitions = definitions;

            // For backwards compatability until the next major release
            SchemaRegistry = new SchemaRegistry(schemaProvider, definitions);
        }

        public ApiDescription ApiDescription { get; private set; }

        public ISchemaProvider SchemaProvider { get; private set; }

        public IDictionary<string, Schema> Definitions { get; private set; }

        [Obsolete("TODO:", false)]
        public ISchemaRegistry SchemaRegistry { get; private set; }
    }
}
