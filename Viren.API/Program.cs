using Viren.API.Extensions;

namespace Viren.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .AddApplicationConfiguration()
            .AddDatabase()
            .AddIdentityAndJwt()
            .AddAwsS3Storage()
            .AddApplicationServices()
            .AddSwaggerAndCors();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            await Viren.Repositories.Data.IdentitySeeder.SeedAsync(app.Services);
        }

        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        });

        app.UseApplicationMiddleware();

        app.Run();
    }
}