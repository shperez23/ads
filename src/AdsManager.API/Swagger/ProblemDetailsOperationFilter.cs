using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AdsManager.API.Swagger;

public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
    private static readonly string[] ErrorStatusCodes =
    [
        StatusCodes.Status400BadRequest.ToString(),
        StatusCodes.Status401Unauthorized.ToString(),
        StatusCodes.Status403Forbidden.ToString(),
        StatusCodes.Status404NotFound.ToString(),
        StatusCodes.Status409Conflict.ToString(),
        StatusCodes.Status500InternalServerError.ToString()
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var statusCode in ErrorStatusCodes)
        {
            if (!operation.Responses.ContainsKey(statusCode))
            {
                operation.Responses[statusCode] = new OpenApiResponse { Description = "Error" };
            }

            operation.Responses[statusCode].Content["application/problem+json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)
            };
        }
    }
}
