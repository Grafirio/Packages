namespace Grafirio.Contracts.AI;

/// <summary>
/// AI grafik verisi talebi iÃ§in message contract
/// </summary>
public interface IGraphDataRequest
{
    Guid RequestId { get; }
    string UserId { get; }
    string CompanyId { get; }
    DateTime RequestTime { get; }
    Dictionary<string, object> Parameters { get; }
}

/// <summary>
/// Grafik verisi talebi concrete implementation
/// </summary>
public record GraphDataRequest(
    Guid RequestId,
    string UserId,
    string CompanyId,
    DateTime RequestTime,
    Dictionary<string, object> Parameters
) : IGraphDataRequest;
