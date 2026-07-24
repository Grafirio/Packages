namespace Grafirio.Contracts.AI;

/// <summary>KonuÅŸma geÃ§miÅŸindeki tek mesaj.</summary>
public record ChatContextItem(string Role, string Content);

/// <summary>
/// AI soru-cevap talebi iÃ§in message contract
/// </summary>
public interface IQuestionRequest
{
    Guid RequestId { get; }
    string UserId { get; }
    string CompanyId { get; }
    DateTime RequestTime { get; }
    string Question { get; }
    List<ChatContextItem>? Context { get; } // Ã–nceki konuÅŸma geÃ§miÅŸi
    string? Database { get; }              // Sorgulanacak veritabanÄ± adÄ±
    List<string>? Tables { get; }          // Ä°lgili tablo listesi (ipucu)
    string? TableName { get; }             // Predictive istekler icin hedef tablo
    Dictionary<string, object>? PredictData { get; } // Predictive istekler icin satir verisi
}

/// <summary>Concrete implementation</summary>
public record QuestionRequest(
    Guid RequestId,
    string UserId,
    string CompanyId,
    DateTime RequestTime,
    string Question,
    List<ChatContextItem>? Context,
    string? Database,
    List<string>? Tables,
    string? TableName,
    Dictionary<string, object>? PredictData
) : IQuestionRequest;
