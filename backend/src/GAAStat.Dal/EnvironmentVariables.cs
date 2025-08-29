namespace GAAStat.Dal;

public static class EnvironmentVariables
{
    public static string DatabaseConnectionString =>
        Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ??
        "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;";
}