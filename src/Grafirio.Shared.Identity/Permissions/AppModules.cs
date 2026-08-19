namespace Grafirio.Shared.Identity.Permissions
{
    /// <summary>
    /// Panelin modülleri: kullanıcının hangi alanlara girebileceği.
    ///
    /// Liste burada, Identity'de değil: modüllerin ikisi (ANALYSIS ve
    /// DATA_SOURCES) DataAnalysis.Api'de uygulanıyor ve anahtarı iki serviste
    /// ayrı ayrı yazmak, birinin diğerinden habersiz değişmesi demekti.
    ///
    /// Rol tavanı burada yok: "hangi rol neye erişebilir" bir politika kararı
    /// ve tek sahibi Identity. Burada duran şey yalnızca sözlük.
    /// </summary>
    public static class AppModules
    {
        /// Kanvas, dashboard, soru sorma. Uygulaması DataAnalysis.Api'de.
        public const string Analysis = "ANALYSIS";

        /// Veri kaynağı bağlama ve yönetme. Uygulaması DataAnalysis.Api'de.
        public const string DataSources = "DATA_SOURCES";

        public const string Documents = "DOCUMENTS";
        public const string CompanySettings = "COMPANY_SETTINGS";

        /// Kim var, kim admin.
        public const string Users = "USERS";

        /// Adlandirilmis izin kumeleri. Onceki adi DEPARTMENTS idi; departman
        /// bir organizasyon birimiydi, bu ise izin kumesi.
        public const string Roles = "ROLES";

        public const string Billing = "BILLING";

        public static readonly string[] All =
        [
            Analysis, DataSources, Documents, CompanySettings, Users, Roles, Billing
        ];

        private static readonly HashSet<string> AllSet = [.. All];

        public static bool IsValid(string? module)
            => !string.IsNullOrWhiteSpace(module) && AllSet.Contains(module);
    }
}
