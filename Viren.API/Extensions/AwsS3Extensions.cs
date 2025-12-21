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

                // =========================
                // 🔍 DEBUG AWS CONFIG
                // =========================
                Console.WriteLine("========== AWS S3 CONFIG ==========");
                Console.WriteLine($"Region       : {opts.Region}");
                Console.WriteLine($"Bucket       : {opts.Bucket}");
                Console.WriteLine($"KeyPrefix    : {opts.KeyPrefix}");
                Console.WriteLine($"PublicBaseUrl: {opts.PublicBaseUrl}");
                Console.WriteLine($"AccessKey    : {(string.IsNullOrEmpty(opts.AccessKey) ? "❌ NOT SET" : "✅ SET")}");
                Console.WriteLine($"SecretKey    : {(string.IsNullOrEmpty(opts.SecretKey) ? "❌ NOT SET" : "✅ SET")}");
                Console.WriteLine("===================================");

                var region = RegionEndpoint.GetBySystemName(opts.Region);

                if (!string.IsNullOrEmpty(opts.AccessKey) &&
                    !string.IsNullOrEmpty(opts.SecretKey))
                {
                    Console.WriteLine("🟢 AWS S3 Client: USING EXPLICIT ACCESS KEY");

                    return new AmazonS3Client(
                        opts.AccessKey,
                        opts.SecretKey,
                        region
                    );
                }

                Console.WriteLine("🟡 AWS S3 Client: USING DEFAULT / IAM ROLE CREDENTIALS");

                // Fallback: IAM Role / Environment credentials
                return new AmazonS3Client(region);
            });

            // Your abstraction
            services.AddScoped<IS3Storage, S3Storage>();

            return builder;
        }
    }
}
