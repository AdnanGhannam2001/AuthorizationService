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

    public static IEnumerable<Client> Clients => [
        new Client {
            ClientId = "WEB_API",
            ClientName = "Web Api Client",
            ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

            RedirectUris = { "http://localhost:5002/signin-oidc" },

            AlwaysIncludeUserClaimsInIdToken = true,

            AllowOfflineAccess = true,
            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile
            }
        }
    ];
}
