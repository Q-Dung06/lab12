using System.Data;
using System.Data.SqlClient;
using Lab12.DTO;

namespace Lab12.DAL;

public sealed class AcademicRepository
{
    public DataTable GetStudentsByFaculty(string facultyCode) => Query(
        "SELECT MaSV,HoTen,MaKhoa,RowVersion FROM dbo.SinhVien WHERE MaKhoa=@MaKhoa AND IsActive=1 ORDER BY MaSV",
        command => command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = facultyCode, "SinhVienTheoKhoa");

    public DataTable GetCourses() => ProcedureTable("dbo.sp_HocPhan_GetAll", null, "HocPhan");

    public DataTable GetEnrollments(string studentCode) => ProcedureTable("dbo.sp_DangKy_GetByStudent", c =>
        c.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = studentCode, "DangKy");

    public DataTable GetTransferHistory(string? studentCode = null) => Query(
        "SELECT MaLichSu,MaSV,MaKhoaCu,MaKhoaMoi,NgayChuyen,LyDo FROM dbo.LichSuChuyenKhoa WHERE @MaSV IS NULL OR MaSV=@MaSV ORDER BY MaLichSu DESC",
        c => c.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = Optional(studentCode), "LichSuChuyenKhoa");

    public int AddCourse(CourseDto course) => ExecuteProcedure("dbo.sp_HocPhan_Insert", c => AddCourseParameters(c, course));
    public int UpdateCourse(CourseDto course) => ExecuteProcedure("dbo.sp_HocPhan_Update", c => AddCourseParameters(c, course));
    public int DeleteCourse(string code) => ExecuteProcedure("dbo.sp_HocPhan_Delete", c => c.Parameters.Add("@MaHP", SqlDbType.VarChar, 10).Value = code);

    public int AddEnrollment(EnrollmentDto enrollment) => ExecuteProcedure("dbo.sp_DangKy_Insert", c => AddEnrollmentParameters(c, enrollment));
    public int UpdateEnrollment(EnrollmentDto enrollment) => ExecuteProcedure("dbo.sp_DangKy_Update", c => AddEnrollmentParameters(c, enrollment));
    public int DeleteEnrollment(string studentCode, string courseCode) => ExecuteProcedure("dbo.sp_DangKy_Delete", c =>
    {
        c.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = studentCode;
        c.Parameters.Add("@MaHP", SqlDbType.VarChar, 10).Value = courseCode;
    });

    public int AddStudentWithEnrollments(StudentDto student, IReadOnlyList<EnrollmentDto> enrollments)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            InsertStudent(student, connection, transaction);
            foreach (var enrollment in enrollments) InsertEnrollment(enrollment, connection, transaction);
            transaction.Commit();
            return enrollments.Count + 1;
        }
        catch
        {
            SafeRollback(transaction);
            throw;
        }
    }

    public RollbackDemoResult DemonstrateRollback(StudentDto student, string facultyCode)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var failed = false;
        try
        {
            InsertStudent(student, connection, transaction);
            // cố ý dùng mã học phần không tồn tại để kích hoạt FK và buộc hoàn tác toàn bộ.
            InsertEnrollment(new EnrollmentDto(student.MaSV, "LAB12_BAD", 1, null), connection, transaction);
            transaction.Commit();
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            failed = true;
            SafeRollback(transaction);
        }
        catch
        {
            SafeRollback(transaction);
            throw;
        }
        var remains = StudentExists(student.MaSV, connection, null);
        return new RollbackDemoResult(failed, !remains && !string.IsNullOrWhiteSpace(facultyCode));
    }

    public void TransferStudent(string studentCode, string targetFaculty, byte[]? originalVersion, string reason)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var oldFaculty = GetStudentFaculty(studentCode, connection, transaction);
            if (oldFaculty is null) throw new DalException(50002, "Không tìm thấy sinh viên đang hoạt động.", "Student missing.");
            if (oldFaculty == targetFaculty) throw new ArgumentException("Sinh viên hiện đã thuộc khoa được chọn.");
            UpdateDepartment(studentCode, oldFaculty, targetFaculty, originalVersion, connection, transaction);
            InsertTransferHistory(studentCode, oldFaculty, targetFaculty, reason, connection, transaction);
            transaction.Commit();
        }
        catch { SafeRollback(transaction); throw; }
    }

    // Chính sách xóa khoa: bắt buộc chọn khoa đích, chuyển toàn bộ sinh viên và ghi nhật ký
    // trong cùng giao dịch trước khi xóa khoa nguồn. Nếu bất kỳ bước nào lỗi thì không đổi dữ liệu.
    public int DeleteFacultySafely(string sourceFaculty, string targetFaculty)
    {
        if (sourceFaculty == targetFaculty) throw new ArgumentException("Khoa đích phải khác khoa cần xóa.");
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            if (!ExistsFaculty(targetFaculty, connection, transaction)) throw new DalException(50013, "Không tìm thấy khoa đích.", "Target faculty missing.");
            var students = ReadStudentVersionsInFaculty(sourceFaculty, connection, transaction);
            foreach (var row in students)
            {
                UpdateDepartment(row.Code, sourceFaculty, targetFaculty, row.Version, connection, transaction);
                InsertTransferHistory(row.Code, sourceFaculty, targetFaculty, "Chuyển khoa để xóa khoa nguồn an toàn", connection, transaction);
            }
            using (var delete = new SqlCommand("DELETE dbo.Khoa WHERE MaKhoa=@MaKhoa", connection, transaction))
            {
                delete.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = sourceFaculty;
                if (delete.ExecuteNonQuery() != 1) throw new DalException(50013, "Không tìm thấy khoa cần xóa.", "Source faculty missing.");
            }
            transaction.Commit();
            return students.Count;
        }
        catch { SafeRollback(transaction); throw; }
    }

    public int UpdateGradesAtomically(IReadOnlyList<GradeChange> changes)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            foreach (var change in changes)
            {
                if (change.Diem is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(changes), $"Điểm của {change.MaSV}/{change.MaHP} nằm ngoài khoảng 0–10.");
                using var update = new SqlCommand("UPDATE dbo.DangKy SET Diem=@Diem WHERE MaSV=@MaSV AND MaHP=@MaHP", connection, transaction);
                var grade = update.Parameters.Add("@Diem", SqlDbType.Decimal); grade.Precision = 4; grade.Scale = 2; grade.Value = change.Diem;
                update.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = change.MaSV;
                update.Parameters.Add("@MaHP", SqlDbType.VarChar, 10).Value = change.MaHP;
                if (update.ExecuteNonQuery() != 1) throw new DalException(50052, "Không tìm thấy đăng ký cần cập nhật điểm.", $"Missing enrollment {change.MaSV}/{change.MaHP}.");
            }
            transaction.Commit();
            return changes.Count;
        }
        catch { SafeRollback(transaction); throw; }
    }

    public bool DemonstrateRowVersionConflict(string studentCode)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var read = new SqlCommand("SELECT MaSV,HoTen,NgaySinh,GioiTinh,MaKhoa,DiemTB,Email,RowVersion FROM dbo.SinhVien WHERE MaSV=@MaSV AND IsActive=1", connection);
        read.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = studentCode;
        using var reader = read.ExecuteReader();
        if (!reader.Read()) throw new DalException(50002, "Không tìm thấy sinh viên đang hoạt động.", $"Missing student {studentCode}.");
        var dto = new StudentDto { MaSV=reader.GetString(0), HoTen=reader.GetString(1), NgaySinh=reader.GetDateTime(2), GioiTinh=reader.GetBoolean(3), MaKhoa=reader.GetString(4), DiemTB=reader.GetDecimal(5), Email=reader.IsDBNull(6)?null:reader.GetString(6), RowVersion=(byte[])reader[7] };
        reader.Close();
        var staleVersion = dto.RowVersion!.ToArray();
        UpdateStudent(dto, connection, null); // giả lập người dùng A đã lưu.
        dto.RowVersion = staleVersion;
        try
        {
            UpdateStudent(dto, connection, null); // giả lập người dùng B gửi phiên bản cũ.
            return false;
        }
        catch (SqlException ex) when (ex.Number == 50031) { return true; }
    }

    public void UpdateDepartment(string studentCode, string oldFaculty, string newFaculty, byte[]? version, SqlConnection connection, SqlTransaction transaction)
    {
        using var update = new SqlCommand("UPDATE dbo.SinhVien SET MaKhoa=@MaKhoa WHERE MaSV=@MaSV AND (@RowVersion IS NULL OR RowVersion=@RowVersion)", connection, transaction);
        update.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = newFaculty;
        update.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = studentCode;
        update.Parameters.Add("@RowVersion", SqlDbType.Binary, 8).Value = version is null ? DBNull.Value : version;
        if (update.ExecuteNonQuery() != 1) throw new DalException(50031, "Sinh viên đã được người khác cập nhật. Tải lại dữ liệu trước khi sửa.", "RowVersion mismatch during department transfer.");
    }

    public void InsertTransferHistory(string studentCode, string oldFaculty, string newFaculty, string reason, SqlConnection connection, SqlTransaction transaction)
    {
        using var insert = new SqlCommand("INSERT dbo.LichSuChuyenKhoa(MaSV,MaKhoaCu,MaKhoaMoi,LyDo) VALUES(@MaSV,@MaKhoaCu,@MaKhoaMoi,@LyDo)", connection, transaction);
        insert.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = studentCode;
        insert.Parameters.Add("@MaKhoaCu", SqlDbType.VarChar, 10).Value = oldFaculty;
        insert.Parameters.Add("@MaKhoaMoi", SqlDbType.VarChar, 10).Value = newFaculty;
        insert.Parameters.Add("@LyDo", SqlDbType.NVarChar, 200).Value = reason;
        insert.ExecuteNonQuery();
    }

    private static void InsertStudent(StudentDto s, SqlConnection connection, SqlTransaction transaction)
    {
        using var c = new SqlCommand("INSERT dbo.SinhVien(MaSV,HoTen,NgaySinh,GioiTinh,MaKhoa,DiemTB,Email,IsActive) VALUES(@MaSV,@HoTen,@NgaySinh,@GioiTinh,@MaKhoa,@DiemTB,@Email,1)", connection, transaction);
        c.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value=s.MaSV; c.Parameters.Add("@HoTen",SqlDbType.NVarChar,100).Value=s.HoTen;
        c.Parameters.Add("@NgaySinh",SqlDbType.Date).Value=s.NgaySinh.Date; c.Parameters.Add("@GioiTinh",SqlDbType.Bit).Value=s.GioiTinh;
        c.Parameters.Add("@MaKhoa",SqlDbType.VarChar,10).Value=s.MaKhoa;
        var score=c.Parameters.Add("@DiemTB",SqlDbType.Decimal); score.Precision=4; score.Scale=2; score.Value=s.DiemTB;
        c.Parameters.Add("@Email",SqlDbType.VarChar,100).Value=Optional(s.Email); c.ExecuteNonQuery();
    }

    private static void InsertEnrollment(EnrollmentDto e, SqlConnection connection, SqlTransaction transaction)
    {
        using var c = new SqlCommand("INSERT dbo.DangKy(MaSV,MaHP,HocKy,Diem) VALUES(@MaSV,@MaHP,@HocKy,@Diem)", connection, transaction);
        c.Parameters.Add("@MaSV",SqlDbType.VarChar,15).Value=e.MaSV; c.Parameters.Add("@MaHP",SqlDbType.VarChar,10).Value=e.MaHP;
        c.Parameters.Add("@HocKy",SqlDbType.Int).Value=e.HocKy;
        var score=c.Parameters.Add("@Diem",SqlDbType.Decimal); score.Precision=4; score.Scale=2; score.Value=e.Diem.HasValue?(object)e.Diem.Value:DBNull.Value;
        c.ExecuteNonQuery();
    }

    private static void UpdateStudent(StudentDto s, SqlConnection connection, SqlTransaction? transaction)
    {
        using var c = new SqlCommand("dbo.sp_SinhVien_Update", connection, transaction) { CommandType=CommandType.StoredProcedure };
        c.Parameters.Add("@MaSV",SqlDbType.VarChar,15).Value=s.MaSV; c.Parameters.Add("@HoTen",SqlDbType.NVarChar,100).Value=s.HoTen;
        c.Parameters.Add("@NgaySinh",SqlDbType.Date).Value=s.NgaySinh.Date; c.Parameters.Add("@GioiTinh",SqlDbType.Bit).Value=s.GioiTinh;
        c.Parameters.Add("@MaKhoa",SqlDbType.VarChar,10).Value=s.MaKhoa;
        var score=c.Parameters.Add("@DiemTB",SqlDbType.Decimal); score.Precision=4; score.Scale=2; score.Value=s.DiemTB;
        c.Parameters.Add("@Email",SqlDbType.VarChar,100).Value=Optional(s.Email); c.Parameters.Add("@OriginalRowVersion",SqlDbType.Binary,8).Value=s.RowVersion!;
        var rows=c.Parameters.Add("@RowsAffected",SqlDbType.Int); rows.Direction=ParameterDirection.Output; c.ExecuteNonQuery();
    }

    private static string? GetStudentFaculty(string code, SqlConnection connection, SqlTransaction transaction)
    {
        using var c=new SqlCommand("SELECT MaKhoa FROM dbo.SinhVien WHERE MaSV=@MaSV AND IsActive=1",connection,transaction);
        c.Parameters.Add("@MaSV",SqlDbType.VarChar,15).Value=code; return c.ExecuteScalar() as string;
    }
    private static bool ExistsFaculty(string code, SqlConnection connection, SqlTransaction transaction)
    {
        using var c=new SqlCommand("SELECT 1 FROM dbo.Khoa WHERE MaKhoa=@MaKhoa",connection,transaction);
        c.Parameters.Add("@MaKhoa",SqlDbType.VarChar,10).Value=code; return c.ExecuteScalar() is not null;
    }
    private static bool StudentExists(string code, SqlConnection connection, SqlTransaction? transaction)
    {
        using var c=new SqlCommand("SELECT 1 FROM dbo.SinhVien WHERE MaSV=@MaSV",connection,transaction);
        c.Parameters.Add("@MaSV",SqlDbType.VarChar,15).Value=code; return c.ExecuteScalar() is not null;
    }
    private static List<(string Code, byte[] Version)> ReadStudentVersionsInFaculty(string code, SqlConnection connection, SqlTransaction transaction)
    {
        using var c=new SqlCommand("SELECT MaSV,RowVersion FROM dbo.SinhVien WITH (UPDLOCK,HOLDLOCK) WHERE MaKhoa=@MaKhoa",connection,transaction);
        c.Parameters.Add("@MaKhoa",SqlDbType.VarChar,10).Value=code; using var r=c.ExecuteReader();
        var rows=new List<(string,byte[])>(); while(r.Read()) rows.Add((r.GetString(0),(byte[])r[1])); return rows;
    }
    private static void SafeRollback(SqlTransaction transaction) { try { if(transaction.Connection is not null) transaction.Rollback(); } catch { } }
    private static object Optional(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static DataTable Query(string sql, Action<SqlCommand> bind, string name)
    {
        using var connection=DbConnectionFactory.Create(); using var command=new SqlCommand(sql,connection); bind(command);
        try { connection.Open(); using var reader=command.ExecuteReader(); var table=new DataTable(name); table.Load(reader); return table; }
        catch(SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }
    private static DataTable ProcedureTable(string procedure, Action<SqlCommand>? bind, string name)
    {
        using var connection=DbConnectionFactory.Create(); using var command=new SqlCommand(procedure,connection){CommandType=CommandType.StoredProcedure}; bind?.Invoke(command);
        try { connection.Open(); using var reader=command.ExecuteReader(); var table=new DataTable(name); table.Load(reader); return table; }
        catch(SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }
    private static int ExecuteProcedure(string procedure, Action<SqlCommand> bind)
    {
        using var connection=DbConnectionFactory.Create(); using var command=new SqlCommand(procedure,connection){CommandType=CommandType.StoredProcedure}; bind(command);
        var rows=command.Parameters.Add("@RowsAffected",SqlDbType.Int); rows.Direction=ParameterDirection.Output;
        try { connection.Open(); command.ExecuteNonQuery(); return Convert.ToInt32(rows.Value); }
        catch(SqlException ex) { throw SqlErrorTranslator.Translate(ex); }
    }
    private static void AddCourseParameters(SqlCommand c, CourseDto x)
    {
        c.Parameters.Add("@MaHP",SqlDbType.VarChar,10).Value=x.MaHP; c.Parameters.Add("@TenHP",SqlDbType.NVarChar,120).Value=x.TenHP; c.Parameters.Add("@SoTinChi",SqlDbType.Int).Value=x.SoTinChi;
    }
    private static void AddEnrollmentParameters(SqlCommand c, EnrollmentDto x)
    {
        c.Parameters.Add("@MaSV",SqlDbType.VarChar,15).Value=x.MaSV; c.Parameters.Add("@MaHP",SqlDbType.VarChar,10).Value=x.MaHP; c.Parameters.Add("@HocKy",SqlDbType.Int).Value=x.HocKy;
        var score=c.Parameters.Add("@Diem",SqlDbType.Decimal); score.Precision=4; score.Scale=2; score.Value=x.Diem.HasValue?(object)x.Diem.Value:DBNull.Value;
    }
}
