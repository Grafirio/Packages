using Grafirio.Shared.Infrastructure.MassTransit.Options;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grafirio.Shared.Infrastructure.MassTransit.Extensions;

public static class MassTransitExt
{
    /// <summary>
    /// RabbitMQ ile MassTransit'i global olarak yapÄ±landÄ±rÄ±r.
    /// </summary>
    /// <param name="configure">Consumer kayÄ±tlarÄ± iÃ§in.</param>
    /// <param name="configureTopology">Exchange/routing key topolojisi iÃ§in (opsiyonel).</param>
    public static IServiceCollection AddGrafirioMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null,
        Action<IRabbitMqBusFactoryConfigurator>? configureTopology = null)
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

                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                ));

                // Servis-spesifik topoloji konfigÃ¼rasyonu (exchange isimleri, tipleri vb.)
                configureTopology?.Invoke(cfg);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
