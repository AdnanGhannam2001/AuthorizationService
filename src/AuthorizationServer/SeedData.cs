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

namespace AuthorizationServer;

public class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();
            
            if (!context.Clients.Any())
            {
                foreach (var client in IdentityServerConfigurations.Clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in IdentityServerConfigurations.IdentityResources)
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in IdentityServerConfigurations.ApiScopes)
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }

        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userFaker = new Faker<ApplicationUser>()
                .RuleFor(x => x.Id, _ => Nanoid.Generate(size: 15))
                .RuleFor(x => x.UserName, f => f.Name.FirstName())
                .RuleFor(x => x.Email, f => f.Internet.Email());
            
            var users = userFaker.Generate(10);

            foreach (var user in users)
            {
                var result = userManager.CreateAsync(user, "Ad@123").Result;

                if (!result.Succeeded)
                {
                    Log.Error("{0}", result.Errors.First().Description);
                    continue;
                }

                result = userManager.AddClaimsAsync(user,
                    [
                        new Claim(JwtClaimTypes.Name, user.UserName!),
                        // TODO
                        new Claim(JwtClaimTypes.BirthDate, "1/1/2001"),
                    ]).Result;

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                Log.Debug("{0} created", user.UserName);
            }
        }
    }
}
