using System.Data;
using System.Data.SqlClient;
using Lab12.DTO;

namespace Lab12.DAL;

public sealed class FacultyRepository
{
    public DataTable GetAll()
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand("dbo.sp_Khoa_GetAll", connection) { CommandType = CommandType.StoredProcedure };
        try
        {
            connection.Open();
            using var reader = command.ExecuteReader();
            var table = new DataTable("Khoa");
            table.Load(reader);
            return table;
        }
        catch (SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }

    public int Insert(FacultyDto faculty) => Execute("dbo.sp_Khoa_Insert", faculty);
    public int Update(FacultyDto faculty) => Execute("dbo.sp_Khoa_Update", faculty);

    public int Delete(string code) => Execute("dbo.sp_Khoa_Delete", code, null);

    private static int Execute(string procedure, FacultyDto faculty) => Execute(procedure, faculty.MaKhoa, faculty.TenKhoa);

    private static int Execute(string procedure, string code, string? name)
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = code;
        if (name is not null) command.Parameters.Add("@TenKhoa", SqlDbType.NVarChar, 100).Value = name;
        var rows = command.Parameters.Add("@RowsAffected", SqlDbType.Int);
        rows.Direction = ParameterDirection.Output;
        try
        {
            connection.Open();
            command.ExecuteNonQuery();
            return Convert.ToInt32(rows.Value);
        }
        catch (SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }
}
