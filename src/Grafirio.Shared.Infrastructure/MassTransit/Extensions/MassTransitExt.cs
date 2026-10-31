using Grafirio.Shared.Infrastructure.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grafirio.Shared.Infrastructure.MassTransit.Extensions;

public static class MassTransitExt
{
    /// <summary>
    /// Configures MassTransit globally. The transport is chosen by <c>Messaging:Transport</c>:
    /// <c>RabbitMq</c> (default, section <c>RabbitMQ</c>) or <c>AzureServiceBus</c>
    /// (section <c>AzureServiceBus</c>). Retry intervals and endpoint conventions are the same for both.
    /// </summary>
    /// <param name="configure">Consumer registrations.</param>
    /// <param name="configureTopology">
    /// Optional per-service bus configuration. It receives the transport-neutral
    /// <see cref="IBusFactoryConfigurator"/>, so it can call things like <c>UseMessageRetry</c> but not
    /// RabbitMQ-only members such as exchange types.
    /// </param>
    public static IServiceCollection AddGrafirioMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null,
        Action<IBusFactoryConfigurator>? configureTopology = null)
    {
        var transport = configuration.GetSection(MessagingOptions.Key).Get<MessagingOptions>()?.Transport
            ?? MessagingOptions.RabbitMq;

        if (string.Equals(transport, MessagingOptions.RabbitMq, StringComparison.OrdinalIgnoreCase))
            return AddRabbitMq(services, configuration, configure, configureTopology);

        if (string.Equals(transport, MessagingOptions.AzureServiceBus, StringComparison.OrdinalIgnoreCase))
            return AddAzureServiceBus(services, configuration, configure, configureTopology);

        throw new InvalidOperationException(
            $"Unknown message transport '{transport}' in '{MessagingOptions.Key}:Transport'. " +
            $"Use '{MessagingOptions.RabbitMq}' or '{MessagingOptions.AzureServiceBus}'.");
    }

    private static IServiceCollection AddRabbitMq(
        IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure,
        Action<IBusFactoryConfigurator>? configureTopology)
    {
        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.Key).Get<RabbitMqOptions>();

        if (rabbitMqOptions is null)
            throw new InvalidOperationException($"RabbitMq configuration section '{RabbitMqOptions.Key}' is missing.");

        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, "/", h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.Durable = true;

                ApplyCommon(cfg, configureTopology);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection AddAzureServiceBus(
        IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure,
        Action<IBusFactoryConfigurator>? configureTopology)
    {
        var serviceBusOptions = configuration.GetSection(AzureServiceBusOptions.Key).Get<AzureServiceBusOptions>();

        if (string.IsNullOrWhiteSpace(serviceBusOptions?.ConnectionString))
            throw new InvalidOperationException(
                $"Azure Service Bus connection string '{AzureServiceBusOptions.Key}:ConnectionString' is missing.");

        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);

            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(serviceBusOptions.ConnectionString);

                ApplyCommon(cfg, configureTopology);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    // Same retry policy for every transport, then the service-specific configuration.
    private static void ApplyCommon(IBusFactoryConfigurator cfg, Action<IBusFactoryConfigurator>? configureTopology)
    {
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)
        ));

        configureTopology?.Invoke(cfg);
    }
}
