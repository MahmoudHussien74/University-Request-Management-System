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

        // 3. Application Business Services
        services.AddScoped<IUniversityRequestService, UniversityRequestService>();
        services.AddScoped<IFormDefinitionService, FormDefinitionService>();

        return services;
    }
}
