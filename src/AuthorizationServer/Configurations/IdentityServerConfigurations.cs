using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace AuthorizationServer.Configurations;

public static class IdentityServerConfigurations
{
    public static IEnumerable<IdentityResource> IdentityResources => [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes => [];
}
