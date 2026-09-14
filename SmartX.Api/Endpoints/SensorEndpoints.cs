using System.IO;
using SmartX.Api.Contracts;
using SmartX.Api.Storage;
using SmartX.Core.Devices;

namespace SmartX.Api.Endpoints;

/// <summary>
/// Sensor Payload Management + Media/Log Attachment requirements: register
/// devices, and attach config files, deployment photos, or hardware logs to
/// a specific sensor profile through a multipart file upload.
/// </summary>
public static class SensorEndpoints
{
    public static void MapSensorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors");

        group.MapPost("/", (RegisterSensorRequest request, SensorRegistry registry) =>
        {
            if (string.IsNullOrWhiteSpace(request.MacAddress))
                return Results.BadRequest(new { Error = "MacAddress is required." });

            var sensor = new SensorDevice
            {
                MacAddress = request.MacAddress.Trim(),
                Location = request.Location?.Trim() ?? string.Empty,
                Category = request.Category,
            };

            registry.Register(sensor);
            return Results.Created($"/api/sensors/{sensor.MacAddress}", sensor);
        });

        group.MapGet("/", (SensorRegistry registry) => Results.Ok(registry.GetAll()));

        group.MapGet("/{mac}", (string mac, SensorRegistry registry) =>
            registry.Get(mac) is { } sensor ? Results.Ok(sensor) : Results.NotFound());

        group.MapDelete("/{mac}", (string mac, SensorRegistry registry) =>
            registry.Remove(mac) ? Results.NoContent() : Results.NotFound());

        // Multipart file upload: config files, deployment photos, hardware logs.
        group.MapPost("/{mac}/attachments", async (string mac, IFormFile file, SensorRegistry registry, IWebHostEnvironment env) =>
        {
            var sensor = registry.Get(mac);
            if (sensor is null)
                return Results.NotFound(new { Error = $"No sensor registered with MAC '{mac}'." });

            if (file.Length == 0)
                return Results.BadRequest(new { Error = "Uploaded file is empty." });

            var safeMacFolder = mac.Replace(":", "").Replace("/", "_");
            var uploadsDir = Path.Combine(env.ContentRootPath, "uploads", safeMacFolder);
            Directory.CreateDirectory(uploadsDir);

            var fileName = Path.GetFileName(file.FileName);
            var savedPath = Path.Combine(uploadsDir, fileName);

            await using (var stream = File.Create(savedPath))
                await file.CopyToAsync(stream);

            sensor.AttachmentPaths.Add(savedPath);

            return Results.Ok(new { sensor.MacAddress, FileName = fileName, file.Length, SavedPath = savedPath });
        }).DisableAntiforgery();

        group.MapGet("/{mac}/attachments", (string mac, SensorRegistry registry) =>
            registry.Get(mac) is { } sensor
                ? Results.Ok(sensor.AttachmentPaths.Select(Path.GetFileName))
                : Results.NotFound());
    }
}
