namespace CoC_API.Endpoints.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapListUsersEndpoint();
        app.MapGetUserEndpoint();
        app.MapEditUserEndpoint();
        app.MapDeactivateUserEndpoint();
        app.MapCreateUserEndpoint();
    }
}
