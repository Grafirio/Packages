namespace Grafirio.Shared.Identity.Permissions
{
    /// <summary>
    /// Bir kullanıcının bir şirketteki etkin yetkisi — Identity'nin
    /// <c>GET /api/v1/permissions/me/{companyId}</c> ucunun gövdesi.
    ///
    /// Hesabı Identity yapıyor: rol tavanı, departman daraltması ve yönetici
    /// muafiyeti orada. Burada duran şey yalnızca sonucun biçimi; iki tarafın
    /// ayrı ayrı tanımladığı bir sözleşme, sessizce ayrışan bir sözleşmedir.
    /// </summary>
    /// <param name="Role">Kullanıcının şirketteki etkin rolü; erişimi yoksa null.</param>
    /// <param name="Modules">Girebileceği modüller — izinlerden türetiliyor.</param>
    /// <param name="Permissions">Etkin izinler (MODÜL.AKSİYON).</param>
    /// <param name="RestrictedByDepartment">
    /// Kümenin departman ataması yüzünden daraltılıp daraltılmadığı.
    /// </param>
    public record EffectivePermissions(
        Guid CompanyId,
        string? Role,
        IReadOnlyList<string> Modules,
        IReadOnlyList<string> Permissions,
        bool RestrictedByDepartment);
}
