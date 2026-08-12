using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using URMS.Application.Contracts.Forms;
using URMS.Application.Contracts.Requests;
using URMS.Application.Services;

namespace URMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Mapster Configuration
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // 2. FluentValidation Registration
        services.AddValidatorsFromAssembly(assembly);

        // 3. Granular Application Domain Services
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IRequestAuthorizationService, RequestAuthorizationService>();
        services.AddScoped<IRequestNotificationService, RequestNotificationService>();
        services.AddScoped<IRequestCreationService, RequestCreationService>();
        services.AddScoped<IRequestQueryService, RequestQueryService>();
        services.AddScoped<IRequestWorkflowService, RequestWorkflowService>();

        // 4. Facade & Business Services Registration
        services.AddScoped<IUniversityRequestService, UniversityRequestService>();
        services.AddScoped<IFormDefinitionService, FormDefinitionService>();

        return services;
    }
}
