namespace Grafirio.Shared.Identity.Permissions
{
    /// <summary>
    /// Bir kullanıcının bir şirketteki etkin yetkisi — Identity'nin
    /// <c>GET /api/v1/permissions/me/{companyId}</c> ucunun gövdesi.
    ///
    /// Hesabı Identity yapıyor: rollerin ve kişisel izinlerin birleşimi,
    /// kurucu ve admin muafiyeti orada. Burada duran şey yalnızca sonucun
    /// biçimi; iki tarafın ayrı ayrı tanımladığı bir sözleşme, sessizce
    /// ayrışan bir sözleşmedir.
    /// </summary>
    /// <param name="Role">
    /// Kullanıcının şirketteki üyelik seviyesi (kurucu, admin, üye); erişimi
    /// yoksa null.
    /// </param>
    /// <param name="Modules">Girebileceği modüller — izinlerden türetiliyor.</param>
    /// <param name="Permissions">Etkin izinler (MODÜL.AKSİYON).</param>
    public record EffectivePermissions(
        Guid CompanyId,
        string? Role,
        IReadOnlyList<string> Modules,
        IReadOnlyList<string> Permissions);
}
