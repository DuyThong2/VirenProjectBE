using Microsoft.EntityFrameworkCore;
using Viren.Repositories;
using Viren.Services.Configs;

namespace Viren.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApplicationConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<PayOsSetings>(
            builder.Configuration.GetSection("PayOS"));
        builder.Services.Configure<FitRoomOptions>(
            builder.Configuration.GetSection("FitRoom"));

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")));

        return builder;
    }
}
