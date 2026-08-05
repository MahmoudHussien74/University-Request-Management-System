using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using URMS.Application.Contracts.Identity;
using URMS.Domain.Entities;
using URMS.Domain.Settings;
using URMS.Infrastructure.Identity;
using URMS.Infrastructure.PermissionAuthorization;
using URMS.Infrastructure.Persistence;

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

            // Read JWT token from HttpOnly Cookie if Authorization header is missing
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("accessToken", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
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
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // ─── 4. Repositories & Unit of Work ───
        services.AddScoped(typeof(URMS.Domain.Contracts.IGenericRepository<>), typeof(URMS.Infrastructure.Persistence.Repositories.GenericRepository<>));
        services.AddScoped<URMS.Application.Contracts.Persistence.IUnitOfWork, URMS.Infrastructure.Persistence.Repositories.UnitOfWork>();

        // ─── 5. Custom Auth, JWT, Permission & Request Services ───
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAdvisorAssignmentService, AdvisorAssignmentService>();
        services.AddScoped<URMS.Application.Contracts.Requests.IUniversityRequestService, URMS.Infrastructure.Services.UniversityRequestService>();
        services.AddScoped<URMS.Application.Contracts.Forms.IFormDefinitionService, URMS.Infrastructure.Services.FormDefinitionService>();

        // ─── 5. Dynamic Permission Policy Provider & Handler ───
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}