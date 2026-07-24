namespace Grafirio.Contracts.AI;

/// <summary>
/// PyCaret analiz talebi â€” LLM tarafÄ±ndan oluÅŸturulan parametrelerle
/// </summary>
public interface IPyCaretAnalysisRequest
{
    Guid RequestId { get; }
    Guid QueryId { get; }
    string CompanyId { get; }
    DateTime RequestTime { get; }

    /// <summary>
    /// BaÄŸlantÄ± bilgileri
    /// </summary>
    string Host { get; }
    int Port { get; }
    string Database { get; }
    string Username { get; }
    string Password { get; }
    bool TrustServerCertificate { get; }

    /// <summary>
    /// LLM tarafÄ±ndan oluÅŸturulan PyCaret config JSON
    /// </summary>
    string ConfigJson { get; }

    /// <summary>
    /// LLM tarafÄ±ndan bu sorgu iÃ§in oluÅŸturulan analiz parametreleri JSON
    /// </summary>
    string AnalysisParamsJson { get; }

    /// <summary>
    /// KullanÄ±cÄ±nÄ±n orijinal sorusu
    /// </summary>
    string UserQuestion { get; }
}

public record PyCaretAnalysisRequest(
    Guid RequestId,
    Guid QueryId,
    string CompanyId,
    DateTime RequestTime,
    string Host,
    int Port,
    string Database,
    string Username,
    string Password,
    bool TrustServerCertificate,
    string ConfigJson,
    string AnalysisParamsJson,
    string UserQuestion
) : IPyCaretAnalysisRequest;
