namespace Grafirio.Shared.Identity.Permissions
{
    public class PermissionOptions
    {
        public const string Key = "PermissionOption";

        /// <summary>
        /// Identity'nin taban adresi (ör. <c>http://identity.api.container:5036</c>).
        /// Gateway üzerinden değil doğrudan: yetki sorusu iç ağda kalıyor ve
        /// gateway'in kendi kimlik doğrulaması araya bir katman daha koyardı.
        /// </summary>
        public string IdentityAddress { get; set; } = string.Empty;

        /// <summary>
        /// Cevabın önbellekte tutulma süresi.
        ///
        /// Tek bir istek içinde aynı soru birkaç kez sorulabiliyor ve her seferi
        /// bir servis atlaması olurdu. Süre kısa tutuluyor: yetki değişikliği
        /// en geç bu kadar gecikmeyle yansımalı, aksi halde bir kullanıcının
        /// yetkisini almak "biraz sonra" anlamına gelir.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(30);
    }
}
