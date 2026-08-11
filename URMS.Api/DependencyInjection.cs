using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using URMS.Api.Middleware;

namespace URMS.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ─── 1. Session Configuration ───
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // ─── 2. CORS Policy Configuration ───
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(
                          "http://localhost:3000",
                          "http://localhost:5174",
                          "https://urms-lake.vercel.app",
                          "http://urms-lake.vercel.app"
                      )
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // ─── 3. Controllers & Exception Handling Filters ───
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // ─── 4. Swagger / OpenAPI Configuration ───
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "University Request Management System (URMS) API",
                Version = "v1",
                Description = "API Documentation for URMS Frontend Integration (Supports English 'en' and Arabic 'ar' via Accept-Language header).\n\n" +
                              "Authentication supported via:\n" +
                              "1. Bearer JWT Token in Authorization Header (`Bearer <token>`)\n" +
                              "2. HttpOnly Cookies (`accessToken` and `refreshToken`)"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT Bearer token format: `Bearer <your_token>`"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            c.AddSecurityRequirement((doc) => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // ─── 5. Request Localization Configuration ───
        var supportedCultures = new[] { "ar-EG", "ar", "en-US", "en" };
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture("ar-EG")
                   .AddSupportedCultures(supportedCultures)
                   .AddSupportedUICultures(supportedCultures);
        });

        return services;
    }
}
