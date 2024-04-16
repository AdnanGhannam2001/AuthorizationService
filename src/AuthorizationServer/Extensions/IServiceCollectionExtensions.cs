using System.Reflection;
using AuthorizationServer.Data;
using AuthorizationServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationServer.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection ConfigureIdentityUser(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection ConfigureIdentityServer(this IServiceCollection services, string connectionString)
    {
        var migrationsAssembly = Assembly.GetExecutingAssembly().GetName().Name;

        services.AddIdentityServer(opts => {
                opts.EmitStaticAudienceClaim = true;

                opts.Events.RaiseErrorEvents = true;
                opts.Events.RaiseInformationEvents = true;
                opts.Events.RaiseFailureEvents = true;
                opts.Events.RaiseSuccessEvents = true;
            })
            .AddConfigurationStore(opts =>
                opts.ConfigureDbContext = b =>
                    b.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)))
            .AddOperationalStore(opts =>
                opts.ConfigureDbContext = b =>
                    b.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)))
            .AddAspNetIdentity<ApplicationUser>();

        return services;
    }

    // TODO
    public static IServiceCollection ConfigureExternalAuth(this IServiceCollection services)
    {
        // services.AddAuthentication()
        //     .AddGoogle(opts => { });

        return services;
    }
}