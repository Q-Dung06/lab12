using System.Configuration;
using System.Data.SqlClient;

namespace Lab12.DAL;

public static class DbConnectionFactory
{
    public static string GetConnectionString()
    {
        var configured = ConfigurationManager.ConnectionStrings["QLSinhVien"]?.ConnectionString;
        if (string.IsNullOrWhiteSpace(configured))
            throw new ConfigurationErrorsException("Thiếu connection string QLSinhVien trong App.config.");
        return configured;
    }

    public static SqlConnection Create() => new(GetConnectionString());
}
