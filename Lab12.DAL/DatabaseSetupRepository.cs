using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Lab12.DAL;

public sealed class DatabaseSetupRepository
{
    private static readonly Regex GoSeparator = new(@"^\s*GO(?:\s+\d+)?\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public int Prepare(string scriptDirectory)
    {
        var appConnectionString = DbConnectionFactory.GetConnectionString();
        var masterConnectionString = new SqlConnectionStringBuilder(appConnectionString) { InitialCatalog = "master" }.ConnectionString;
        var count = ExecuteScript(masterConnectionString, Path.Combine(scriptDirectory, "01_QLSinhVien.sql"));
        count += ExecuteScript(appConnectionString, Path.Combine(scriptDirectory, "02_SeedForPaging.sql"));
        count += ExecuteScript(appConnectionString, Path.Combine(scriptDirectory, "03_Lab12_StoredProcedures.sql"));
        count += ExecuteScript(appConnectionString, Path.Combine(scriptDirectory, "04_Lab12_MasterDetailTransactions.sql"));
        return count;
    }

    private static int ExecuteScript(string connectionString, string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Không tìm thấy script chuẩn bị CSDL.", path);
        var script = File.ReadAllText(path);
        var batches = GoSeparator.Split(script).Select(batch => batch.Trim()).Where(batch => batch.Length > 0);
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        var count = 0;
        foreach (var batch in batches)
        {
            using var command = new SqlCommand(batch, connection) { CommandTimeout = 90 };
            command.ExecuteNonQuery();
            count++;
        }
        return count;
    }
}
