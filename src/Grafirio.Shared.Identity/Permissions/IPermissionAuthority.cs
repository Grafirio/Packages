namespace Grafirio.Shared.Identity.Permissions
{
    /// <summary>
    /// "Bu kullanıcı şu şirkette şunu yapabilir mi" sorusunun servisler arası
    /// cevabı.
    ///
    /// Yetkinin kaynağı Identity'nin veritabanı, token değil: izinler
    /// claim'lere yazılsaydı bir kullanıcının yetkisini almak token süresi
    /// kadar gecikirdi. Bu yüzden soru her seferinde Identity'ye gidiyor
    /// (kısa ömürlü önbellekle).
    /// </summary>
    public interface IPermissionAuthority
    {
        /// <summary>
        /// Belirli bir iznin verilip verilmediği. Şirkete erişim kontrolünü de
        /// kapsıyor: erişimi olmayanın rolü null, rolü null olanın izni yok.
        ///
        /// Identity'ye ulaşılamazsa <c>false</c> döner. Yetki kararı sorulamıyorsa
        /// verilmez — panelin tersi: orada izin okunamayınca menü gizlenmiyor,
        /// çünkü orada karar değil görünüm söz konusu.
        /// </summary>
        Task<bool> CanAsync(Guid companyId, string permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kullanıcının şirketteki bütün yetkisi. Erişimi yoksa ya da soru
        /// cevaplanamadıysa null.
        /// </summary>
        Task<EffectivePermissions?> ForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    }
}
