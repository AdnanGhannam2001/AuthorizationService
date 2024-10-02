namespace AuthorizationServer.Constants;

internal static class DatabaseConstants
{
#if DOCKER
    public const string UsersConnectionStringName = "DockerUsersConnection";
    public const string ConfigsConnectionStringName = "DockerConfigsConnection";
#else
    public const string UsersConnectionStringName = "UsersConnection";
    public const string ConfigsConnectionStringName = "ConfigsConnection";
#endif // DOCKER 
}