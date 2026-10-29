using FluentValidation;
using Focus.Application.Common.Behaviors;
using Focus.Application.Common.Options;
using Focus.Application.Features.Session.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Focus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<AlgorithmOptions>();
        services.AddScoped<SessionSettlementService>();

        return services;
    }
}
