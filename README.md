<<<<<<< HEAD 1
# Smart-X — Task 1 (Sensor Data Ingestion and Telemetry)

> Built for .NET 10. This sandbox does not have the .NET SDK installed, so these
> files have been written carefully but *not compiled here* — build and run
> them in Visual Studio 2022 (17.10+) or the dotnet CLI on your own machine.

## Solution layout


SmartX.sln
SmartX.Core/      <- shared class library: generics, operator overloading,
                     recursion, jagged arrays, health/streak engine
SmartX.Api/        <- ASP.NET Core Minimal API: ingestion, sensor
                     registration, file upload, simulation seeding
SmartX.Client/     <- WPF dashboard (THIS STEP): landing menu, sensor
                     registration + file attach, live health cards
## Opening the solution

If `SmartX.sln` doesn't load cleanly in your Visual Studio version, regenerate
it in one minute instead of debugging the file by hand:

```bash
cd SmartX
dotnet new sln -n SmartX --force
dotnet sln add SmartX.Core/SmartX.Core.csproj
dotnet sln add SmartX.Api/SmartX.Api.csproj
dotnet sln add SmartX.Client/SmartX.Client.csproj
```

## Running the API

```bash
cd SmartX.Api
dotnet run
```
It starts on **http://localhost:5080** (fixed in `Properties/launchSettings.json`
so the WPF client, added in the next step, can point at a known address).
Open `SmartX.Api.http` in Visual Studio, VS Code (REST Client extension), or
Rider and click "Send Request" above each block to try every endpoint,
including the `/api/simulation/seed/{mac}?count=10000` stress test.
To test the multipart file-upload endpoint (not expressible cleanly in a
`.http` file) from a terminal instead:

```bash
curl -F "file=@./some-config.json" http://localhost:5080/api/sensors/24:6F:28:AE:11:9C/attachments
```
## Requirement → endpoint map (SmartX.Api)

| Brief requirement | Endpoint(s) |
|---|---|
| API Integration Layer (primary telemetry receiver) | `POST /api/telemetry/{mac}/moisture` \| `/power` \| `/switch` |
| Sensor Payload Management (MAC, location, category) | `POST` / `GET /api/sensors` |
| Media/Log Attachment (multipart upload) | `POST /api/sensors/{mac}/attachments` |
| Dynamic engagement feature (Health Score + Streak) | `GET /api/telemetry/{mac}/health` |
| Jagged-array batch stats (feeds anomaly detection) | `GET /api/telemetry/{mac}/stats` |
| Operator overloading in action (`PowerReading` `+`) | `GET /api/power/aggregate` |
| Seed heavy mock data to prove structures scale | `POST /api/simulation/seed/{mac}?count=10000` |
| Recursion in action (nested deployment validation) | `POST /api/deployment/validate` |
`SensorRegistry` and `TelemetryStore` (in `Storage/`) are the only two
services registered in `Program.cs` - both are in-memory singletons, which is
appropriate for a simulated gateway assignment but is clearly marked in code
comments as a simplification versus a real persistence layer.
## Running the full stack (SmartX.Client)

**SmartX.Client requires Windows** (WPF only runs on Windows, even with
`dotnet run` from the CLI). If you're on Windows with the .NET 10 SDK and the
".NET desktop development" workload installed in Visual Studio:
1. Start the API first: `cd SmartX.Api && dotnet run` (leave it running).
2. Run the client: `cd SmartX.Client && dotnet run`, or press F5 on
   `SmartX.Client` in Visual Studio (set it as the startup project).
3. On the landing menu, click the **Sensor Data Ingestion and Telemetry**
   tile (the other two are intentionally disabled/greyed out per the brief).
4. Register a sensor, then use the buttons under "Act on Sensor":
   - **Attach Config File / Photo / Log...** opens a native `OpenFileDialog`
     and uploads the chosen file via the API's multipart endpoint.
   - **Send One Live Reading** posts a single randomised moisture reading.
   - **Stress-Test: Seed 10,000 Readings** calls
     `/api/simulation/seed/{mac}` to push 10,000 readings through the real
     ingestion path in one shot - watch the Activity Log for the elapsed
     milliseconds it took, and the health card update almost instantly.
The health cards on the right poll `GET /api/telemetry/{mac}/health` every 3
seconds and re-render the `RadialGaugeControl` (a hand-drawn WPF arc, not a
`ProgressBar`) plus the streak text and colour state live.
## Requirement → file map (SmartX.Client)

| Brief requirement | File |
|---|---|
| Startup landing page / 3 pillars, 2 disabled | `MainWindow.xaml` |
| Sensor registration form | `Views/IngestionWindow.xaml` (+ `.cs`) |
| File attach via `OpenFileDialog` | `Views/IngestionWindow.xaml.cs` (`AttachFileButton_Click`) |
| Dynamic engagement feature, not a progress bar | `Controls/RadialGaugeControl.xaml(.cs)` |
| Talks to the API layer to push/retrieve data | `Services/ApiClient.cs` |
## Requirement → file map (SmartX.Core)

| Technical requirement | File |
|---|---|
| **Generics** — `TelemetryPacket<T>` uniform wrapper, no boxing | `Telemetry/TelemetryPacket.cs` |
| **Operator overloading** — `+`, `-`, `>`, `<`, `==` on sensor data | `Telemetry/PowerReading.cs` |
| **Advanced arrays & lists** — jagged array batches → `List<T>` | `Telemetry/TelemetryBatchBuffer.cs` |
| **Recursion** — nested deployment-tree validation | `Devices/DeploymentValidator.cs` |
| Sensor registration record | `Devices/SensorDevice.cs` |
| Dynamic engagement feature engine (Health Score + Streak) | `Diagnostics/SensorHealth.cs` |
| Mock data seeding for load-testing the structures above | `Simulation/MockTelemetryGenerator.cs` |
## Try it (once you have the .NET 10 SDK)

Drop this into a scratch `Program.cs` in a console project referencing
`SmartX.Core` to sanity-check the pieces interactively:

```csharp
using SmartX.Core.Telemetry;
using SmartX.Core.Devices;
using SmartX.Core.Diagnostics;
// Operator overloading
var meter1 = new PowerReading("M1", 820, DateTimeOffset.UtcNow);
var meter2 = new PowerReading("M2", 640, DateTimeOffset.UtcNow);
var meter3 = meter1 + meter2;
Console.WriteLine($"Aggregate load: {meter3}");
Console.WriteLine($"M1 draws more than M2? {meter1 > meter2}");

using SmartX.Core.Simulation;
// Generics + no-boxing packets
var moisture = MockTelemetryGenerator.GenerateMoistureReadings("ESP32-014", 20, DateTimeOffset.UtcNow).ToList();
Console.WriteLine(moisture.First());
// Jagged array batching -> List<T>, then stats
var buffer = MockTelemetryGenerator.SeedBatchBuffer(10_000);
var (mean, stdDev) = buffer.ComputeStatistics();
Console.WriteLine($"Seeded {buffer.Count} readings. Mean={mean:F2}, StdDev={stdDev:F2}");
// Recursive deployment validation
var tree = new DeploymentNode
{
    Name = "Facility A",
    Children =
    {
        new DeploymentNode
        {
            Name = "Zone 1",
            Children =
            {
                new DeploymentNode
                {
                    Name = "Sub-Zone B",
                    Sensors = { new SensorDevice { MacAddress = "24:6F:28:AE:11:9C", Category = SensorCategory.Environmental } }
                }
            }
        }
    }
};
var result = DeploymentValidator.Validate(tree);
Console.WriteLine($"Deployment valid: {result.IsValid}");
// Health score / streak engagement engine
var health = new SensorHealth("ESP32-014");
health.RecordReading(withinExpectedRange: true, DateTimeOffset.UtcNow);
Console.WriteLine($"Score={health.Score}, State={health.State}");
```

