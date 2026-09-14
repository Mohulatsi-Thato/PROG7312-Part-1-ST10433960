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
