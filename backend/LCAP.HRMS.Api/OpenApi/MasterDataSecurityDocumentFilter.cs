using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LCAP.HRMS.Api.OpenApi;

// Document master-data authentication without marking anonymous health as protected.
public sealed class MasterDataSecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var path in document.Paths.Where(path =>
            path.Key.StartsWith("/api/companies", StringComparison.Ordinal)
            || path.Key.StartsWith("/api/branches", StringComparison.Ordinal)
            || path.Key.StartsWith("/api/departments", StringComparison.Ordinal)
            || path.Key.StartsWith("/api/designations", StringComparison.Ordinal)
            || path.Key.StartsWith("/api/shifts", StringComparison.Ordinal)
            || path.Key.StartsWith("/api/work-locations", StringComparison.Ordinal)))
        foreach (var operation in path.Value.Operations!.Values)
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            }];
    }
}
