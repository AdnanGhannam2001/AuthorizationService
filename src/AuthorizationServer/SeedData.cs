using System.Security.Claims;
using IdentityModel;
using AuthorizationServer.Data;
using AuthorizationServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Bogus;
using NanoidDotNet;
using AuthorizationServer.Configurations;
using SocialMediaService.WebApi.Protos;
using Google.Protobuf.WellKnownTypes;
using Duende.IdentityServer.Models;

namespace AuthorizationServer;

public static class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        var seedConfigurationsTask = SeedConfigurationsAsync(app);
        var seedUsersTask = SeedUsersAsync(app);

        Task.WhenAll(seedConfigurationsTask, seedUsersTask).Wait();
    }

    private static async Task SeedConfigurationsAsync(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        await scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        if (!await context.Clients.AnyAsync())
        {
            var clients = new List<Client>();
            app.Configuration.GetSection("Clients").Bind(clients);

            foreach (var client in clients)
            {
                foreach (var secret in client.ClientSecrets)
                {
                    secret.Value = secret.Value.Sha256();
                }

                await context.Clients.AddAsync(client.ToEntity());
            }

            await context.SaveChangesAsync();
        }

        if (!await context.IdentityResources.AnyAsync())
        {
            foreach (var resource in IdentityServerConfigurations.IdentityResources)
            {
                await context.IdentityResources.AddAsync(resource.ToEntity());
            }

            await context.SaveChangesAsync();
        }

        if (!await context.ApiScopes.AnyAsync())
        {
            foreach (var resource in IdentityServerConfigurations.ApiScopes)
            {
                await context.ApiScopes.AddAsync(resource.ToEntity());
            }

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedUsersAsync(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();
        await context.Users.ExecuteDeleteAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var profileService = app.Services.GetRequiredService<ProfileService.ProfileServiceClient>();

        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(x => x.Id, _ => Nanoid.Generate(size: 15))
            .RuleFor(x => x.UserName, f => f.Name.FirstName())
            .RuleFor(x => x.Email, f => f.Internet.Email());

        var profileFaker = new Faker<CreateProfileRequest>()
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.DateOfBirth, f =>
            {
                var start = new DateTime(1980, 1, 1);
                var end = new DateTime(2010, 1, 1);
                return Timestamp.FromDateTime(f.Date.Between(start, end).ToUniversalTime());
            })
            .RuleFor(x => x.Gender, f => f.PickRandom<Genders>())
            .RuleFor(x => x.PhoneNumber, _ => "123-456-7891");

        const int generated = 10;
        var users = userFaker.Generate(generated);
        var profiles = profileFaker.Generate(generated);

        for (var i = 0; i < generated; i++)
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await userManager.CreateAsync(users[i], "Ad@123");

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                profiles[i].Id = users[i].Id;
                var profileResult = await profileService.CreateProfileAsync(profiles[i]);

                result = await userManager.AddClaimsAsync(users[i], [new Claim(JwtClaimTypes.Name, users[i].UserName!)]);

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                await transaction.CommitAsync();
                Log.Debug("{0} created", users[i].UserName);
            }
            catch (Exception exp)
            {
                await transaction.RollbackAsync();
                Log.Error("{0}", exp.Message);
            }
        }
    }
}
