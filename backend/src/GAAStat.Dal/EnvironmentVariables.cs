namespace GAAStat.Dal;

public static class EnvironmentVariables
{
    public static string DatabaseConnectionString =>
        Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ??
        "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;Include Error Detail=true;";

    public static int MaxFileSizeMb =>
        int.TryParse(Environment.GetEnvironmentVariable("MAX_FILE_SIZE_MB"), out var size) ? size : 50;

    public static string FileUploadPath =>
        Environment.GetEnvironmentVariable("FILE_UPLOAD_PATH") ??
        Path.Combine(Path.GetTempPath(), "gaastat-uploads");

    public static int DefaultJobListPageSize =>
        int.TryParse(Environment.GetEnvironmentVariable("DEFAULT_JOB_LIST_PAGE_SIZE"), out var pageSize) ? pageSize : 20;

    public static int MaxJobListPageSize =>
        int.TryParse(Environment.GetEnvironmentVariable("MAX_JOB_LIST_PAGE_SIZE"), out var maxPageSize) ? maxPageSize : 100;
}