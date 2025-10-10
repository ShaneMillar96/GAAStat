namespace GAAStat.Dal;

public static class EnvironmentVariables
{
    public static string DatabaseConnectionString =>
        Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ??
        "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=20;Connection Lifetime=60;Connection Idle Lifetime=30;Command Timeout=30;";
}