using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Brizbee.Api
{
    public class AuthorizationHeaderOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Validate operation
            if (operation == null) { throw new ArgumentNullException(nameof(operation)); }

            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // Authorization header
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "JWT",
                Required = false
            });
        }
    }
}
