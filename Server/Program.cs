namespace Server;

public static class Program
{
    public static ConnectionManagerV1 ConnectionManagerV1 { get; private set; } = null!;

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        WebApplication app = builder.Build();

        WebSocketOptions options = new()
        {
            KeepAliveTimeout = TimeSpan.FromMinutes(2),
        };

        app.UseWebSockets(options);

        CancellationToken exitToken = app.Lifetime.ApplicationStopping;
        
        ConnectionManagerV1 = new ConnectionManagerV1(exitToken);

        app.Map("/v1/ws", ConnectionManagerV1.OnRequest);
        
        app.Run();
    }
}
