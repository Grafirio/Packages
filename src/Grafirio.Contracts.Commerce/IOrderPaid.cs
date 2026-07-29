namespace Grafirio.Contracts.Commerce;

/// <summary>
/// Ödemesi başarıyla alınmış bir sipariş.
///
/// Ticaret tarafı abonelik kavramını bilmez; yalnızca neyin satın alındığını
/// bildirir. Bunun bir erişim hakkına dönüşüp dönüşmeyeceğine kimlik tarafı
/// karar verir. Böylece ödeme sağlayıcısı değiştiğinde ya da abonelik kuralları
/// evrildiğinde iki taraf birbirini sürüklemez.
/// </summary>
public interface IOrderPaid
{
    Guid OrderId { get; }
    string OrderCode { get; }

    /// <summary>Ödemeyi yapan kullanıcının Keycloak kimliği.</summary>
    string BuyerId { get; }

    Guid PaymentId { get; }
    decimal Amount { get; }
    DateTime PaidAt { get; }

    /// <summary>
    /// Siparişteki kalemlerden abonelik planına karşılık gelenler.
    /// Sıradan ürün siparişlerinde boştur.
    /// </summary>
    IReadOnlyList<string> SubscriptionPlans { get; }
}

/// <summary>Concrete implementation</summary>
public record OrderPaid(
    Guid OrderId,
    string OrderCode,
    string BuyerId,
    Guid PaymentId,
    decimal Amount,
    DateTime PaidAt,
    IReadOnlyList<string> SubscriptionPlans
) : IOrderPaid;
