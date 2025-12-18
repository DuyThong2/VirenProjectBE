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
            .AddApplicationServices()
            .AddSwaggerAndCors();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            await Viren.Repositories.Data.IdentitySeeder.SeedAsync(app.Services);
        }

        app.UseApplicationMiddleware();

        app.Run();
    }
}