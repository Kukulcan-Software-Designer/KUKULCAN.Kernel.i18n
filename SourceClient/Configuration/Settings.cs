namespace ATLAS.Kernel.i18n.Client.Configuration;

// ── API connection ────────────────────────────────────────────────────────────
public sealed class ApiSettings
{
    public const string SectionKey = "Api";

    public string BaseUrl    { get; set; } = "http://localhost:5000";
    public string BearerToken { get; set; } = string.Empty;
}

// ── Database (mirrors Atlas.Kernel.Database AtlasDatabaseOptions) ─────────────
public sealed class AtlasDatabaseSettings
{
    public const string SectionKey = "Atlas:Database";

    public DatabaseProvider Provider              { get; set; } = DatabaseProvider.PostgreSql;
    public string           ConnectionString      { get; set; } = string.Empty;
    public int              CommandTimeoutSeconds { get; set; } = 30;
    public bool             EnableSensitiveDataLogging { get; set; } = false;
    public bool             EnableDetailedErrors  { get; set; } = false;
    public RetrySettings    Retry                 { get; set; } = new();
    public PoolSettings     Pool                  { get; set; } = new();
    public MigrationSettings Migration            { get; set; } = new();

    public sealed class RetrySettings
    {
        public bool Enabled             { get; set; } = true;
        public int  MaxRetryCount       { get; set; } = 3;
        public int  MaxRetryDelaySeconds { get; set; } = 30;
    }

    public sealed class PoolSettings
    {
        public bool Enabled { get; set; } = true;
        public int  MinSize { get; set; } = 5;
        public int  MaxSize { get; set; } = 100;
    }

    public sealed class MigrationSettings
    {
        public bool AutoMigrateOnStartup { get; set; } = false;
        public bool SeedDataOnStartup    { get; set; } = true;
    }
}

// ── Supported DB providers ────────────────────────────────────────────────────
public enum DatabaseProvider
{
    SqlServer  = 0,
    PostgreSql = 1,
    MySql      = 2
}
