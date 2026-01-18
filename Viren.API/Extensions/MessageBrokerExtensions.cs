using MassTransit;
using Microsoft.Extensions.Options;
using System.Reflection;
using Viren.Services.Configs;

namespace Viren.API.Extensions
{
    public static class MessageBrokerExtensions
    {
        public static WebApplicationBuilder AddMessageBroker(
            this WebApplicationBuilder builder,
            Assembly? consumersAssembly = null)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Bind MessageBrokerSettings from appsettings
            services.Configure<MessageBrokerSettings>(
                configuration.GetSection(nameof(MessageBrokerSettings))
            );

            // Expose IMessageBrokerSettings
            services.AddSingleton<IMessageBrokerSettings>(sp =>
                sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

            services.AddMassTransit(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();

                if (consumersAssembly != null)
                    cfg.AddConsumers(consumersAssembly);

                cfg.UsingRabbitMq((context, rmq) =>
                {
                    var settings = context.GetRequiredService<IMessageBrokerSettings>();

                    rmq.Host(new Uri(settings.Host), host =>
                    {
                        host.Username(settings.UserName);
                        host.Password(settings.Password);
                    });

                    rmq.ConfigureEndpoints(context);
                });
            });

            return builder;
        }
    }
}
