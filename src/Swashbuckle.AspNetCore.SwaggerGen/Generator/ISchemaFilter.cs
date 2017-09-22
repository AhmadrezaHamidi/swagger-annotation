using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public interface ISchemaFilter
    {
        void Apply(Schema model, SchemaFilterContext context);
    }

    public class SchemaFilterContext
    {
        public SchemaFilterContext(
            Type systemType,
            JsonContract jsonContract,
            ISchemaProvider schemaProvider,
            IDictionary<string, Schema> definitions)
        {
            SystemType = systemType;
            JsonContract = jsonContract;
            SchemaProvider = schemaProvider;
            Definitions = definitions;

            // For backwards compatability until the next major release
            SchemaRegistry = new SchemaRegistry(schemaProvider, definitions);
        }

        public Type SystemType { get; private set; }

        public JsonContract JsonContract { get; private set; }

        public ISchemaProvider SchemaProvider { get; }

        public IDictionary<string, Schema> Definitions { get; }

        [Obsolete("TODO:", false)]
        public ISchemaRegistry SchemaRegistry { get; private set; }
    }
}