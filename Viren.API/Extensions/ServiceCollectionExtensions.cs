using Microsoft.Extensions.Options;
using Net.payOS;
using Viren.API.Services;
using Viren.Repositories.Impl;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.Configs;
using Viren.Services.Impl;
using Viren.Services.IntegrationEvents;
using Viren.Services.Interfaces;
using Viren.Services.Outbox;
using Viren.Services.Workers;

namespace Viren.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.Configure<PayOsSetings>(builder.Configuration.GetSection("PayOS"));

        builder.Services.AddScoped<IUser, CurrentUser>();
        builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<ITokenRepository, TokenRepository>();
        builder.Services.AddScoped<IUserService, UserService>();

        builder.Services.AddScoped<IProductService, ProductServiceWithVectorOutbox>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IProductDetailService, ProductDetailServiceWithVectorOutbox>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddHttpClient<IFitRoomService, FitRoomService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FitRoomOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                throw new InvalidOperationException("FitRoom:BaseUrl chưa được cấu hình.");
            }

            client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/");
            client.DefaultRequestHeaders.Remove("X-API-KEY");
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);
            }
        });
        builder.Services.AddHttpClient<IMeshyService, MeshyService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var apiKey = config["Meshy:ApiKey"];
            var baseUrl = config["Meshy:BaseUrl"];

            client.BaseAddress = new Uri(baseUrl);

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        builder.Services.AddHostedService<OutboxPublisherWorker>();

        builder.Services.AddScoped<IEventBusPublisher, MassTransitEventBusPublisher>();

        builder.Services.AddControllers();
        builder.Services.AddRouting(o => o.LowercaseUrls = true);

        return builder;
    }
}
