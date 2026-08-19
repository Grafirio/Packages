namespace Grafirio.Shared.Identity.Permissions
{
    /// <summary>
    /// İzin anahtarları: <c>MODÜL.AKSİYON</c>.
    ///
    /// Modül tek başına açık/kapalı olduğunda "bağlantıyı görsün ama
    /// değiştirmesin" ya da "kullanıcı listesini görsün ama rol atamasın"
    /// ifade edilemiyordu; aksiyon kırılımı bunun için var.
    ///
    /// Sözlük burada, politika Identity'de: hangi rolün hangi izinlere
    /// çıkabildiği (tavan) ve departmanın bunu nasıl daralttığı Identity'nin
    /// kararı. Diğer servisler yalnızca "şu izin verilmiş mi" diye soruyor —
    /// bkz. <see cref="IPermissionAuthority"/>.
    ///
    /// Anahtar biçimi metin: Identity'nin deposu MongoDB, şemasız ve migration
    /// yok. Enum kullanmak, alanı taşımayan eski belgelerin okunmasını kıran
    /// bir değer tipi eklemek olurdu.
    /// </summary>
    public static class AppPermissions
    {
        /// <summary>
        /// Panele giriş. Üyelikle birlikte geliyor ve izin kümesiyle
        /// kaldırılamıyor: yanlış tanımlanmış bir rol, kullanıcıyı boş bir ekran
        /// yerine kapının dışında bırakmasın.
        /// </summary>
        public const string PanelRead = "PANEL.READ";

        public const string AnalysisRead = "ANALYSIS.READ";

        /// Soru sormak, analiz koşturmak.
        public const string AnalysisCreate = "ANALYSIS.CREATE";
        public const string AnalysisDelete = "ANALYSIS.DELETE";

        public const string DataSourcesRead = "DATA_SOURCES.READ";

        /// Sunucu bağlamak.
        public const string DataSourcesCreate = "DATA_SOURCES.CREATE";

        /// Bağlantıyı değiştirmek; tablo seçimi de buraya giriyor.
        public const string DataSourcesUpdate = "DATA_SOURCES.UPDATE";
        public const string DataSourcesDelete = "DATA_SOURCES.DELETE";

        public const string DocumentsRead = "DOCUMENTS.READ";
        public const string DocumentsCreate = "DOCUMENTS.CREATE";
        public const string DocumentsDelete = "DOCUMENTS.DELETE";

        public const string CompanySettingsRead = "COMPANY_SETTINGS.READ";
        public const string CompanySettingsUpdate = "COMPANY_SETTINGS.UPDATE";

        /// <summary>
        /// Alt şirket açmak. Şirket bilgisini düzeltmekten ayrı: yeni bir tüzel
        /// kişilik açmak aynı ağırlıkta bir iş değil.
        /// </summary>
        public const string CompanySettingsCreateChild = "COMPANY_SETTINGS.CREATE_CHILD";

        public const string UsersRead = "USERS.READ";

        /// Kullanıcı kaydı.
        public const string UsersCreate = "USERS.CREATE";

        /// <summary>
        /// Birini admin yapmak ya da adminliğini geri almak.
        /// <see cref="UsersCreate"/>'ten ayrı: kullanıcı açmak günlük bir iş,
        /// kimin izin şemasının dışına çıkacağına karar vermek değil.
        /// </summary>
        public const string UsersManageMembership = "USERS.MANAGE_MEMBERSHIP";

        public const string RolesRead = "ROLES.READ";
        public const string RolesCreate = "ROLES.CREATE";
        public const string RolesUpdate = "ROLES.UPDATE";
        public const string RolesDelete = "ROLES.DELETE";

        /// Kullanıcıya rol vermek, geri almak.
        public const string RolesAssign = "ROLES.ASSIGN";

        /// <summary>
        /// Bir rolün izin kümesini değiştirmek ve kişiye özel izin vermek.
        /// Rol atamaktan ayrı: atamak var olan bir kümeyi kullanmak, bunu
        /// düzenlemek ise yetkinin kendisini şekillendirmek.
        /// </summary>
        public const string RolesManagePermissions = "ROLES.MANAGE_PERMISSIONS";

        public const string BillingRead = "BILLING.READ";
        public const string BillingManage = "BILLING.MANAGE";

        public static readonly string[] All =
        [
            PanelRead,
            AnalysisRead, AnalysisCreate, AnalysisDelete,
            DataSourcesRead, DataSourcesCreate, DataSourcesUpdate, DataSourcesDelete,
            DocumentsRead, DocumentsCreate, DocumentsDelete,
            CompanySettingsRead, CompanySettingsUpdate, CompanySettingsCreateChild,
            UsersRead, UsersCreate, UsersManageMembership,
            RolesRead, RolesCreate, RolesUpdate, RolesDelete,
            RolesAssign, RolesManagePermissions,
            BillingRead, BillingManage
        ];

        private static readonly HashSet<string> AllSet = [.. All];

        public static bool IsValid(string? permission)
            => !string.IsNullOrWhiteSpace(permission) && AllSet.Contains(permission);

        /// <summary>
        /// Anahtarın modül parçası: <c>"DATA_SOURCES.UPDATE"</c> →
        /// <c>"DATA_SOURCES"</c>. <see cref="PanelRead"/>'in
        /// <see cref="AppModules"/>'de karşılığı yok, onun için null döner.
        /// </summary>
        public static string? ModuleOf(string permission)
        {
            var dot = permission.IndexOf('.');
            if (dot <= 0) return null;

            var module = permission[..dot];
            return AppModules.IsValid(module) ? module : null;
        }

        /// Bir modülün bütün izinleri.
        public static IReadOnlyList<string> ForModule(string module) =>
            [.. All.Where(p => p.StartsWith(module + ".", StringComparison.Ordinal))];

        /// <summary>
        /// İzin listesinin dokunduğu modüller — menüyü doldurmak için.
        /// </summary>
        public static List<string> ModulesOf(IEnumerable<string> permissions) =>
            [.. permissions
                .Select(ModuleOf)
                .Where(m => m is not null)
                .Select(m => m!)
                .Distinct()];
    }
}
