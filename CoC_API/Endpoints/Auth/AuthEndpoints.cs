namespace CoC_API.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapLoginEndpoint();
        app.MapMeEndpoint();
        app.MapLogoutEndpoint();
    }
}
