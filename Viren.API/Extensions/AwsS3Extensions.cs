using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Storage.Settings;

namespace Viren.API.Extensions
{
    public static class AwsS3Extensions
    {
        public static WebApplicationBuilder AddAwsS3Storage(
            this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Bind AwsOptions from appsettings
            services.Configure<AwsOptions>(
                configuration.GetSection(nameof(AwsOptions))
            );

            // Expose IAwsOptions
            services.AddSingleton<IAwsOptions>(sp =>
                sp.GetRequiredService<IOptions<AwsOptions>>().Value);

            // Amazon S3 client
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var opts = sp.GetRequiredService<IAwsOptions>();
                var region = RegionEndpoint.GetBySystemName(opts.Region);

                if (!string.IsNullOrEmpty(opts.AccessKey) &&
                    !string.IsNullOrEmpty(opts.SecretKey))
                {

                    return new AmazonS3Client(
                        opts.AccessKey,
                        opts.SecretKey,
                        region
                    );
                }
                return new AmazonS3Client(region);
            });

            services.AddScoped<IS3Storage, S3Storage>();

            return builder;
        }
    }
}
