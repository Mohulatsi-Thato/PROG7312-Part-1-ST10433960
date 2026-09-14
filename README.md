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
