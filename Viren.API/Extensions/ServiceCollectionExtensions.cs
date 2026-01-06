using Net.payOS;
using Viren.API.Services;
using Viren.Repositories.Impl;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.Configs;
using Viren.Services.Impl;
using Viren.Services.Interfaces;

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
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IProductDetailService, ProductDetailService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IOrderService, OrderService>();

        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();


        builder.Services.AddControllers();
        builder.Services.AddRouting(o => o.LowercaseUrls = true);

        return builder;
    }
}