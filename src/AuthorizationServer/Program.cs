using System.Diagnostics;
using AuthorizationServer;
using AuthorizationServer.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(ctx.Configuration));

    var usersDbConnectionString = builder.Configuration.GetConnectionString("UsersConnection");
    var configsDbConnectionString = builder.Configuration.GetConnectionString("ConfigsConnection");

    Debug.Assert(usersDbConnectionString != null && configsDbConnectionString != null);

    builder.Services.AddRazorPages();

    builder.Services
        .ConfigureIdentityUser(usersDbConnectionString)
        .ConfigureIdentityServer(configsDbConnectionString)
        .ConfigureExternalAuth();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseIdentityServer();
    app.UseAuthorization();
    
    app.MapRazorPages()
        .RequireAuthorization();

    if (args.Contains("--seed"))
    {
        Log.Information("Seeding database...");
        SeedData.EnsureSeedData(app);
        Log.Information("Done seeding database. Exiting.");
        return;
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}