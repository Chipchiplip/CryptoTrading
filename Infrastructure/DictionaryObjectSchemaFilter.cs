using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CryptoTrading.Infrastructure
{
    /// <summary>
    /// Schema filter to handle Dictionary with string keys and object values, and plain object types in Swagger
    /// </summary>
    public class DictionaryObjectSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Handle Dictionary<string, object>
            if (context.Type == typeof(Dictionary<string, object>))
            {
                schema.Type = "object";
                schema.AdditionalPropertiesAllowed = true;
                schema.AdditionalProperties = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalPropertiesAllowed = true
                };
                schema.Example = new OpenApiObject
                {
                    ["key1"] = new OpenApiString("value1"),
                    ["key2"] = new OpenApiInteger(42),
                    ["key3"] = new OpenApiBoolean(true)
                };
            }
            // Handle plain object type
            else if (context.Type == typeof(object))
            {
                schema.Type = "object";
                schema.AdditionalPropertiesAllowed = true;
                schema.Example = new OpenApiObject
                {
                    ["property"] = new OpenApiString("any value")
                };
            }
        }
    }
}

