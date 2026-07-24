namespace Grafirio.Contracts.AI;

/// <summary>
/// AI soru-cevap yanÄ±tÄ± iÃ§in message contract
/// </summary>
public interface IQuestionResponse
{
    Guid RequestId { get; }
    bool Success { get; }
    string? ErrorMessage { get; }
    DateTime ResponseTime { get; }
    string? Answer { get; }
    Dictionary<string, object>? Metadata { get; } // Confidence score, sources, vb.
}

/// <summary>
/// Soru-cevap yanÄ±tÄ± concrete implementation
/// </summary>
public record QuestionResponse(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    DateTime ResponseTime,
    string? Answer,
    Dictionary<string, object>? Metadata
) : IQuestionResponse;
