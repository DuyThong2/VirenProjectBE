using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS.Constants;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Viren.API.Services;
using Viren.Repositories.Utils;
using Viren.Services.Configs;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Viren.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<PayOsSetings>(builder.Configuration.GetSection("PayOS"));

        


        //builder.Services.AddSingleton(sp =>
        //{
        //    var cfg = sp.GetRequiredService<IConfiguration>();
        //    var connStr = cfg["Azure:BlobStorageSettings:ConnectionString"]
        //                  ?? throw new ArgumentException("Missing Azure Blob connection string");
        //    return new BlobServiceClient(connStr);
        //});
        // Add custom exception handling middleware


        builder.Services.AddProblemDetails();
        //builder.Services.AddExceptionHandler<CustomExceptionHandler>();

        //builder.Services.AddDbContext<DbContext>((sp, options) =>
        //{
        //    options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        //});
        //builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DbContext>());
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddScoped<IUser, CurrentUser>();



        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddRouting(options => options.LowercaseUrls = true);


        builder.Services.AddEndpointsApiExplorer();

        //Add Identity
        //builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        //{
        //    options.Password.RequireDigit = true;
        //    options.Password.RequiredLength = 8;
        //    options.Password.RequireNonAlphanumeric = true;
        //    options.Password.RequireUppercase = true;
        //    options.Password.RequireLowercase = true;
        //    options.Password.RequiredUniqueChars = 1;
        //})

        //.AddDefaultTokenProviders()
        //.AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("Default")
        //.AddEntityFrameworkStores<DbContext>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Viren API",
                Version = "v1",
                Description = "API for Viren application"
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    []!
                }
            });
        });

        builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(5));
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var config = builder.Configuration;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireAudience = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidAudience = config["JwtSettings:Audience"],
                ValidIssuer = config["JwtSettings:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"] ?? throw new ArgumentException()))
            };
        });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Configure forwarded headers for reverse proxy scenarios
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
        // }
        app.UseForwardedHeaders();

        app.UseRouting();

        app.UseCors("AllowAll");

        app.UseExceptionHandler();

        using (var scope = app.Services.CreateScope())
        {
            //var db = scope.ServiceProvider.GetRequiredService<DbContext>();
            //db.Database.Migrate();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        // app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();


        app.Run();
    }
}