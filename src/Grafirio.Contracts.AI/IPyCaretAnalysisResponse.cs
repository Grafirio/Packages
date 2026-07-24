namespace Grafirio.Contracts.AI;

/// <summary>
/// PyCaret analiz sonucu â€” grafik verileri ve insights
/// </summary>
public interface IPyCaretAnalysisResponse
{
    Guid RequestId { get; }
    Guid QueryId { get; }
    string CompanyId { get; }
    DateTime ResponseTime { get; }
    bool Success { get; }
    string? Error { get; }

    /// <summary>
    /// Grafik verileri JSON â€” [{type, title, data: {labels, datasets}}]
    /// </summary>
    string ChartsJson { get; }

    /// <summary>
    /// Analiz bulgularÄ± JSON â€” [{type, title, description}]
    /// </summary>
    string InsightsJson { get; }

    /// <summary>
    /// Analiz Ã¶zeti (metin)
    /// </summary>
    string Summary { get; }
}

public record PyCaretAnalysisResponse(
    Guid RequestId,
    Guid QueryId,
    string CompanyId,
    DateTime ResponseTime,
    bool Success,
    string? Error,
    string ChartsJson,
    string InsightsJson,
    string Summary
) : IPyCaretAnalysisResponse;
