namespace Grafirio.Contracts.AI;

/// <summary>
/// AI veri analizi yanÄ±tÄ± iÃ§in message contract
/// </summary>
public interface IDataAnalysisResponse
{
    Guid RequestId { get; }
    string UserId { get; }
    bool Success { get; }
    string? ErrorMessage { get; }
    DateTime ResponseTime { get; }
    
    // Analysis Results
    AnalysisResultData? Results { get; }
}

/// <summary>
/// Analiz sonuÃ§ verisi
/// </summary>
public record AnalysisResultData
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<ChartData> Charts { get; init; } = new();
    public List<InsightData> Insights { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Chart verisi
/// </summary>
public record ChartData
{
    public string Type { get; init; } = string.Empty; // "bar", "line", "pie", "scatter"
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// AI Insight verisi
/// </summary>
public record InsightData
{
    public string Type { get; init; } = string.Empty; // "info", "warning", "success", "danger"
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Veri analizi yanÄ±tÄ± concrete implementation
/// </summary>
public record DataAnalysisResponse(
    Guid RequestId,
    string UserId,
    bool Success,
    string? ErrorMessage,
    DateTime ResponseTime,
    AnalysisResultData? Results
) : IDataAnalysisResponse;
