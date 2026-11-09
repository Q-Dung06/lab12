using System.Data;
using System.Data.SqlClient;
using Lab12.DTO;

namespace Lab12.DAL;

public sealed class StudentRepository
{
    public DataTable Search(string? keyword, string? facultyCode, bool includeInactive)
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand("dbo.sp_SinhVien_Search", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@TuKhoa", SqlDbType.NVarChar, 100).Value = Optional(keyword);
        command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = Optional(facultyCode);
        command.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive;
        try
        {
            connection.Open();
            using var reader = command.ExecuteReader();
            var table = new DataTable("SinhVien");
            table.Load(reader);
            return table;
        }
        catch (SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }

    public int Insert(StudentDto student) => ExecuteStudent("dbo.sp_SinhVien_Insert", student);
    public int Update(StudentDto student) => ExecuteStudent("dbo.sp_SinhVien_Update", student);

    public int SoftDelete(string code) => Execute("dbo.sp_SinhVien_Delete", command =>
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = code,
        hasRowsOutput: true);

    public int Restore(string code) => Execute("dbo.sp_SinhVien_Restore", command =>
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = code,
        hasRowsOutput: true);

    private static int ExecuteStudent(string procedure, StudentDto student) => Execute(procedure, command =>
    {
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = student.MaSV;
        command.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = student.HoTen;
        command.Parameters.Add("@NgaySinh", SqlDbType.Date).Value = student.NgaySinh.Date;
        command.Parameters.Add("@GioiTinh", SqlDbType.Bit).Value = student.GioiTinh;
        command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = student.MaKhoa;
        var score = command.Parameters.Add("@DiemTB", SqlDbType.Decimal);
        score.Precision = 4;
        score.Scale = 2;
        score.Value = student.DiemTB;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = Optional(student.Email);
        if (procedure.EndsWith("_Update", StringComparison.OrdinalIgnoreCase))
        {
            if (student.RowVersion is not { Length: 8 })
                throw new InvalidOperationException("Thiếu RowVersion gốc để cập nhật an toàn.");
            command.Parameters.Add("@OriginalRowVersion", SqlDbType.Binary, 8).Value = student.RowVersion;
        }
    }, hasRowsOutput: true);

    private static int Execute(string procedure, Action<SqlCommand> addParameters, bool hasRowsOutput)
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        addParameters(command);
        SqlParameter? rowsOutput = null;
        if (hasRowsOutput)
        {
            rowsOutput = command.Parameters.Add("@RowsAffected", SqlDbType.Int);
            rowsOutput.Direction = ParameterDirection.Output;
        }
        try
        {
            connection.Open();
            command.ExecuteNonQuery();
            return rowsOutput is null ? 0 : Convert.ToInt32(rowsOutput.Value);
        }
        catch (SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }

    private static object Optional(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
