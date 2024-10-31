using System.Diagnostics;
using AuthorizationServer;
using AuthorizationServer.Constants;
using AuthorizationServer.Extensions;
using Serilog;
using SocialMediaService.WebApi.Protos;

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

    var usersDbConnectionString = builder.Configuration.GetConnectionString(DatabaseConstants.UsersConnectionStringName);
    var configsDbConnectionString = builder.Configuration.GetConnectionString(DatabaseConstants.ConfigsConnectionStringName);

    Debug.Assert(usersDbConnectionString != null && configsDbConnectionString != null);

    builder.Services.AddRazorPages();

    builder.Services
        .ConfigureIdentityUser(usersDbConnectionString)
        .ConfigureIdentityServer(configsDbConnectionString)
        .ConfigureExternalAuth()
        .AddGrpcClient<ProfileService.ProfileServiceClient>(config =>
        {
            var host = builder.Configuration.GetValue<string>("Grpc:ProfileServiceHost") ?? throw new NullReferenceException("Grpc:ProfileServiceHost should be defined in configs");
            config.Address = new Uri(host);
        });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors(x =>
    {
        x
            .SetIsOriginAllowed(x => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    app.UseIdentityServer();
    app.UseAuthorization();

    app.MapRazorPages()
        .RequireAuthorization();

    if (args.Contains("-c") || args.Contains("--clear"))
    {
        Log.Information("Clearing database...");
        SeedData.EnsureCleared(app);
        Log.Information("Done clearing database. Exiting.");
        return;
    }

    if (args.Contains("-s") || args.Contains("--seed"))
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