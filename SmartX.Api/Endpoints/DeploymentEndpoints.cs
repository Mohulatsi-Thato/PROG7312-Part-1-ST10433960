using SmartX.Core.Devices;

namespace SmartX.Api.Endpoints;

/// <summary>
/// Exposes the Core library's recursive deployment-tree validator over HTTP,
/// so the nested "Sub-Zone B -> Zone 1 -> Facility A" configuration can be
/// checked either from the WPF client's registration screen or directly via
/// this endpoint (e.g. from the included <c>SmartX.Api.http</c> file, Postman,
/// or curl) without needing the desktop client running.
/// </summary>
public static class DeploymentEndpoints
{
    public static void MapDeploymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/deployment").WithTags("Deployment");

        group.MapPost("/validate", (DeploymentNode tree) =>
        {
            var result = DeploymentValidator.Validate(tree);
            return result.IsValid ? Results.Ok(result) : Results.BadRequest(result);
        });

        // POST (not GET) because it carries the whole tree as its request body.
        group.MapPost("/find", (DeploymentNode tree, string name) =>
        {
            var path = DeploymentValidator.FindPath(tree, name);
            return path is not null ? Results.Ok(new { Path = path }) : Results.NotFound();
        });
    }
}
