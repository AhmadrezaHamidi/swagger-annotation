using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen.Test
{
    public class RecursiveCallSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema model, SchemaFilterContext context)
        {
            model.Properties = new Dictionary<string, Schema>();
#pragma warning disable CS0618 // Type or member is obsolete
            model.Properties.Add("ExtraProperty", context.SchemaRegistry.GetOrRegister(typeof(ComplexType)));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
