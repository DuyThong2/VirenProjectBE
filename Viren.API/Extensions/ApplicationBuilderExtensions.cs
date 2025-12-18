using Microsoft.EntityFrameworkCore;
using Viren.Repositories;

namespace Viren.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApplicationConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<Viren.Services.Configs.PayOsSetings>(
            builder.Configuration.GetSection("PayOS"));

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