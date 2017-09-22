using System;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen.Test
{
    public class VendorExtensionsDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Extensions.Add("X-property1", "value");
#pragma warning disable CS0618 // Type or member is obsolete
            context.SchemaRegistry.GetOrRegister(typeof(DateTime));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}