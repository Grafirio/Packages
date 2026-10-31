using Grafirio.Shared.Infrastructure.MassTransit.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grafirio.Shared.Infrastructure.Tests;

/// <summary>
/// The bus is built but never started, so no broker is contacted. Values are synthetic.
/// </summary>
public sealed class MassTransitTransportTests
{
    private const string SyntheticServiceBus =
        "Endpoint=sb://synthetic-test.servicebus.windows.net/;SharedAccessKeyName=synthetic;SharedAccessKey=c3ludGhldGljLXRlc3Qta2V5LW5vdC1yZWFs";

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();

    private static (IBus Bus, IBusFactoryConfigurator? Seen) Build(
        IConfiguration configuration, Action<IBusFactoryConfigurator>? topology = null)
    {
        IBusFactoryConfigurator? seen = null;
        var services = new ServiceCollection().AddLogging();
        services.AddGrafirioMassTransit(configuration, configureTopology: cfg =>
        {
            seen = cfg;
            topology?.Invoke(cfg);
        });
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IBus>(), seen);
    }

    private static readonly (string, string)[] Rabbit =
    [
        ("RabbitMQ:Host", "synthetic-rabbit"), ("RabbitMQ:Username", "u"), ("RabbitMQ:Password", "p"),
    ];

    [Fact]
    public void Without_a_transport_setting_RabbitMq_is_used()
    {
        var (bus, seen) = Build(Config(Rabbit));

        Assert.Equal("rabbitmq", bus.Address.Scheme);
        Assert.IsAssignableFrom<global::MassTransit.IRabbitMqBusFactoryConfigurator>(seen);
    }

    [Theory]
    [InlineData("RabbitMq")]
    [InlineData("rabbitmq")]
    public void RabbitMq_can_be_selected_explicitly_and_case_does_not_matter(string transport)
    {
        var (bus, _) = Build(Config([.. Rabbit, ("Messaging:Transport", transport)]));

        Assert.Equal("rabbitmq", bus.Address.Scheme);
    }

    [Fact]
    public void AzureServiceBus_is_used_when_selected_and_ignores_the_RabbitMq_section()
    {
        var (bus, seen) = Build(Config(
            ("Messaging:Transport", "AzureServiceBus"),
            ("AzureServiceBus:ConnectionString", SyntheticServiceBus)));

        Assert.Equal("sb", bus.Address.Scheme);
        Assert.Contains("synthetic-test.servicebus.windows.net", bus.Address.Host);
        Assert.IsAssignableFrom<global::MassTransit.IServiceBusBusFactoryConfigurator>(seen);
    }

    [Fact]
    public void The_service_topology_callback_runs_for_both_transports()
    {
        var rabbitCalls = 0;
        var serviceBusCalls = 0;

        Build(Config(Rabbit), _ => rabbitCalls++);
        Build(Config(("Messaging:Transport", "AzureServiceBus"), ("AzureServiceBus:ConnectionString", SyntheticServiceBus)),
            _ => serviceBusCalls++);

        Assert.Equal(1, rabbitCalls);
        Assert.Equal(1, serviceBusCalls);
    }

    [Fact]
    public void A_missing_Service_Bus_connection_string_fails_at_startup_without_echoing_secrets()
    {
        var failure = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGrafirioMassTransit(Config(("Messaging:Transport", "AzureServiceBus"))));

        Assert.Contains("AzureServiceBus:ConnectionString", failure.Message);
    }

    [Fact]
    public void A_missing_RabbitMq_section_still_fails_as_before()
    {
        var failure = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGrafirioMassTransit(Config(("Unrelated:Setting", "x"))));

        Assert.Contains("RabbitMQ", failure.Message);
    }

    [Fact]
    public void An_unknown_transport_is_rejected_by_name()
    {
        var failure = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGrafirioMassTransit(Config(("Messaging:Transport", "Kafka"))));

        Assert.Contains("Kafka", failure.Message);
        Assert.Contains("RabbitMq", failure.Message);
        Assert.Contains("AzureServiceBus", failure.Message);
    }
}
