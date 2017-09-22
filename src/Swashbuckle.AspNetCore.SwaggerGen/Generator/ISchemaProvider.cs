using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public interface ISchemaProvider
    {
        Schema GetSchema(Type type, IDictionary<string, Schema> definitions);
    }
}
