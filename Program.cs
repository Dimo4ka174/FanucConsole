using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FanucFocasConsole.Services;
using FanucFocasConsole.Interop;
using FanucFocasConsole.DB;
using Serilog.Events;
using DotNetEnv;
using Serilog;

// Load .env (if present) before building the configuration.
Env.Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/fanuc-test-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("=== FanucFocasConsole starting ===");
    Log.Information("Platform: {Platform}", RuntimeInformation.OSDescription);

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();
        })
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<IFocasNative>(FocasNativeFactory.Create());

            var connString = context.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is not set. Cannot start.");
            }

            services.AddSingleton<FanucRepository>(sp =>
                new FanucRepository(connString, sp.GetRequiredService<ILogger<FanucRepository>>()));

            services.AddTransient<FanucDataService>();
        })
        .Build();

    // Database is optional. Failure here does not stop FOCAS polling.
    var repo = host.Services.GetRequiredService<FanucRepository>();
    bool dbAvailable = true;
    try
    {
        await repo.EnsureDatabaseExistsAsync();
        await repo.EnsureCreatedAsync();
        Log.Information("Database and tables are ready.");
    }
    catch (Exception ex)
    {
        dbAvailable = false;
        Log.Error(ex, "Failed to initialize the database. Continuing without persistence.");
    }

    var service = host.Services.GetRequiredService<FanucDataService>();

    // Resolve machine IPs: command line > env vars > interactive prompt.
    List<string> ips = new();

    if (args.Length > 0)
    {
        ips.AddRange(args);
    }
    else
    {
        var ipsEnv = Environment.GetEnvironmentVariable("MACHINE_IPS");
        if (!string.IsNullOrWhiteSpace(ipsEnv))
        {
            ips.AddRange(ipsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else
        {
            var ipEnv = Environment.GetEnvironmentVariable("MACHINE_IP");
            if (!string.IsNullOrWhiteSpace(ipEnv))
                ips.Add(ipEnv);
        }
    }

    if (ips.Count == 0)
    {
        Console.Write("Enter machine IP (or several, comma-separated): ");
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
            ips.AddRange(input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    if (ips.Count == 0)
    {
        Log.Error("No machine IP provided. Use command-line args, MACHINE_IPS env var, or interactive input.");
        return;
    }

    ushort port = ushort.TryParse(Environment.GetEnvironmentVariable("MACHINE_PORT"), out var p) ? p : (ushort)8193;
    int timeout = int.TryParse(Environment.GetEnvironmentVariable("MACHINE_TIMEOUT"), out var t) ? t : 10;

    foreach (var ip in ips)
    {
        Log.Information("--- Processing machine {Ip} ---", ip);
        try
        {
            await service.RunTestAsync(ip, port, timeout, dbAvailable);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while polling machine {Ip}", ip);
        }
    }

    Log.Information("Application finished.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal application error");
}
finally
{
    Log.CloseAndFlush();
}
