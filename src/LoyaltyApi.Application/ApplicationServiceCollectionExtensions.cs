using FluentValidation;
using LoyaltyApi.Application.Behaviors;
using LoyaltyApi.Application.Mapping;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LoyaltyApi.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        MappingConfig.Configure();

        return services;
    }
}
