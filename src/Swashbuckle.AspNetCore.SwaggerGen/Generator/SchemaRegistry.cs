using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    // NOTE: The SchemaRegistry concept and implementation has now been replaced by ISchemaProvider/SchemaGenerator
    // But, for backwards compatibility until the next major release, continue to maintain the ISchemaRegistry
    // interface (exposed in filter interfaces) through an adapter to the new implementation
    public interface ISchemaRegistry
    {
        Schema GetOrRegister(Type type);

        IDictionary<string, Schema> Definitions { get; }
    }

    public class SchemaRegistry : ISchemaRegistry
    {
        private readonly ISchemaProvider _schemaProvider;

        public SchemaRegistry(ISchemaProvider schemaProvider, IDictionary<string, Schema> definitions)
        {
            _schemaProvider = schemaProvider;
            Definitions = definitions;
        }

        public Schema GetOrRegister(Type type)
        {
            return _schemaProvider.GetSchema(type, Definitions);
        }

        public IDictionary<string, Schema> Definitions { get; private set; }
    }
}
