using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using URMS.Application.Common.Models;
using URMS.Application.Contracts.Identity;
using URMS.Application.Settings;
using URMS.Infrastructure.Identity;
using URMS.Infrastructure.PermissionAuthorization;
using URMS.Infrastructure.Services;

namespace URMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ─── Options Pattern: Bind JwtSettings from appsettings.json ───
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // ─── 1. DbContext ───
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // ─── 2. ASP.NET Identity ───
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // ─── 3. JWT & Cookie Dual Authentication ───
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing in appsettings.json");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Read JWT token from HttpOnly Cookie if Authorization header is missing & return localized 401/403 responses
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue(AuthConstants.AccessTokenCookie, out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();
                    var message = localizer?.GetLocalizedString("UnauthorizedAccess") ?? "عفواً، يجب تسجيل الدخول للوصول إلى هذه الصفحة.";
                    var response = ApiResponse.Failure(message, [new ApiError("UnauthorizedAccess", message)], StatusCodes.Status401Unauthorized);

                    await context.Response.WriteAsJsonAsync(response);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();
                    var message = localizer?.GetLocalizedString("ForbiddenAccess") ?? "عفواً، ليس لديك صلاحية لتنفيذ هذا الإجراء.";
                    var response = ApiResponse.Failure(message, [new ApiError("ForbiddenAccess", message)], StatusCodes.Status403Forbidden);

                    await context.Response.WriteAsJsonAsync(response);
                }
            };
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "URMS.AuthSession";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();
                var message = localizer?.GetLocalizedString("UnauthorizedAccess") ?? "عفواً، يجب تسجيل الدخول للوصول إلى هذه الصفحة.";
                var response = ApiResponse.Failure(message, [new ApiError("UnauthorizedAccess", message)], StatusCodes.Status401Unauthorized);

                await context.Response.WriteAsJsonAsync(response);
            };
            options.Events.OnRedirectToAccessDenied = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();
                var message = localizer?.GetLocalizedString("ForbiddenAccess") ?? "عفواً، ليس لديك صلاحية لتنفيذ هذا الإجراء.";
                var response = ApiResponse.Failure(message, [new ApiError("ForbiddenAccess", message)], StatusCodes.Status403Forbidden);

                await context.Response.WriteAsJsonAsync(response);
            };
        });

        // ─── 4. Repositories & Unit of Work ───
        services.AddScoped(typeof(URMS.Domain.Contracts.IGenericRepository<>), typeof(URMS.Infrastructure.Persistence.Repositories.GenericRepository<>));
        services.AddScoped<URMS.Application.Contracts.Persistence.IUnitOfWork, URMS.Infrastructure.Persistence.Repositories.UnitOfWork>();

        // ─── Domain-Specific Repositories ───
        services.AddScoped<URMS.Application.Contracts.Persistence.IUniversityRequestRepository, URMS.Infrastructure.Persistence.Repositories.UniversityRequestRepository>();
        services.AddScoped<URMS.Application.Contracts.Persistence.IFormDefinitionRepository, URMS.Infrastructure.Persistence.Repositories.FormDefinitionRepository>();

        // ─── 5. Custom Auth, JWT, Permission, Request & Localization Services ───
        services.AddHttpContextAccessor();
        services.AddLocalization();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAdvisorAssignmentService, AdvisorAssignmentService>();
        services.AddScoped<IEmailService, EmailService>();

        // ─── 6. Dynamic Permission Policy Provider & Handler ───
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ─── 7. Background Services ───
        services.AddHostedService<URMS.Infrastructure.Services.RefreshTokenCleanupService>();

        return services;
    }
}