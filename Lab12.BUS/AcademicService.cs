using System.Data;
using System.Net.Mail;
using Lab12.DAL;
using Lab12.DTO;

namespace Lab12.BUS;

public sealed class AcademicService
{
    private readonly AcademicRepository _repository = new();

    public DataTable StudentsByFaculty(string? facultyCode) => Run(() => _repository.GetStudentsByFaculty(Required(facultyCode, 10, "Chọn khoa trước.")));
    public DataTable Courses() => Run(_repository.GetCourses);
    public DataTable Enrollments(string? studentCode) => Run(() => _repository.GetEnrollments(Required(studentCode, 15, "Chọn sinh viên trước.")));
    public DataTable TransferHistory(string? studentCode = null) => Run(() => _repository.GetTransferHistory(studentCode));

    public int AddCourse(string? code, string? name, int credits) => Run(() => _repository.AddCourse(ValidateCourse(code, name, credits)));
    public int UpdateCourse(string? code, string? name, int credits) => Run(() => _repository.UpdateCourse(ValidateCourse(code, name, credits)));
    public int DeleteCourse(string? code) => Run(() => _repository.DeleteCourse(Required(code, 10, "Chọn học phần cần xóa.")));

    public int AddEnrollment(string? studentCode, string? courseCode, int semester, decimal? grade) =>
        Run(() => _repository.AddEnrollment(ValidateEnrollment(studentCode, courseCode, semester, grade)));
    public int UpdateEnrollment(string? studentCode, string? courseCode, int semester, decimal? grade) =>
        Run(() => _repository.UpdateEnrollment(ValidateEnrollment(studentCode, courseCode, semester, grade)));
    public int DeleteEnrollment(string? studentCode, string? courseCode) => Run(() => _repository.DeleteEnrollment(Required(studentCode,15,"Chọn sinh viên."), Required(courseCode,10,"Chọn học phần cần gỡ.")));

    public int AddStudentWithEnrollments(StudentDto student, IReadOnlyList<EnrollmentDto> enrollments)
    {
        ValidateStudent(student);
        if (enrollments.Count == 0) throw new BusinessRuleException("Hãy chọn ít nhất một học phần đăng ký ban đầu.");
        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in enrollments)
        {
            if (item.MaSV != student.MaSV) throw new BusinessRuleException("Mã sinh viên trong danh sách đăng ký không khớp.");
            if (item.HocKy is < 1 or > 3) throw new BusinessRuleException("Học kỳ phải từ 1 đến 3.");
            if (!distinct.Add(item.MaHP)) throw new BusinessRuleException("Danh sách đăng ký có học phần bị lặp.");
            ValidateGrade(item.Diem);
        }
        return Run(() => _repository.AddStudentWithEnrollments(student, enrollments));
    }

    public RollbackDemoResult DemonstrateRollback(string? facultyCode)
    {
        var faculty = Required(facultyCode, 10, "Chọn khoa cho dữ liệu thử.");
        var student = new StudentDto { MaSV = "RB" + DateTime.Now.ToString("HHmmssfff"), HoTen = "Sinh viên kiểm tra rollback", NgaySinh = DateTime.Today.AddYears(-20), GioiTinh = true, MaKhoa = faculty, DiemTB = 7m, Email = null };
        return Run(() => _repository.DemonstrateRollback(student, faculty));
    }

    public void TransferStudent(string? studentCode, string? targetFaculty, byte[]? version, string? reason)
    {
        var student = Required(studentCode,15,"Chọn sinh viên cần chuyển khoa.");
        var target = Required(targetFaculty,10,"Chọn khoa đích.");
        var why = string.IsNullOrWhiteSpace(reason) ? "Chuyển khoa theo yêu cầu" : reason.Trim();
        if (why.Length > 200) throw new BusinessRuleException("Lý do chuyển khoa tối đa 200 ký tự.");
        Run(() => { _repository.TransferStudent(student,target,version,why); return 0; });
    }

    public int DeleteFacultySafely(string? source, string? target) =>
        Run(() => _repository.DeleteFacultySafely(Required(source,10,"Chọn khoa cần xóa."),Required(target,10,"Chọn khoa nhận sinh viên.")));

    public int UpdateGradesAtomically(IReadOnlyList<GradeChange> changes)
    {
        if (changes.Count == 0) throw new BusinessRuleException("Nhập ít nhất một dòng MaSV,MaHP,Diem.");
        return Run(() => _repository.UpdateGradesAtomically(changes));
    }

    public bool DemonstrateRowVersionConflict(string? studentCode) => Run(() => _repository.DemonstrateRowVersionConflict(Required(studentCode,15,"Nhập mã sinh viên đang hoạt động.")));

    private static CourseDto ValidateCourse(string? code, string? name, int credits)
    {
        code = Required(code,10,"Mã học phần không được để trống."); name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120) throw new BusinessRuleException("Tên học phần phải có từ 1 đến 120 ký tự.");
        if (credits is < 1 or > 6) throw new BusinessRuleException("Số tín chỉ phải từ 1 đến 6.");
        return new CourseDto(code,name,credits);
    }
    private static EnrollmentDto ValidateEnrollment(string? student, string? course, int semester, decimal? grade)
    {
        if (semester is < 1 or > 3) throw new BusinessRuleException("Học kỳ phải từ 1 đến 3.");
        ValidateGrade(grade);
        return new EnrollmentDto(Required(student,15,"Mã sinh viên không được để trống."),Required(course,10,"Mã học phần không được để trống."),semester,grade);
    }
    private static void ValidateStudent(StudentDto s)
    {
        s.MaSV=Required(s.MaSV,15,"Mã sinh viên không được để trống.");
        s.HoTen=s.HoTen.Trim(); if(s.HoTen.Length is 0 or >100) throw new BusinessRuleException("Họ tên phải có từ 1 đến 100 ký tự.");
        if(s.NgaySinh>DateTime.Today.AddYears(-15)||s.NgaySinh<DateTime.Today.AddYears(-120)) throw new BusinessRuleException("Ngày sinh không hợp lệ; sinh viên phải từ 15 tuổi trở lên.");
        s.MaKhoa=Required(s.MaKhoa,10,"Chọn khoa hợp lệ.");
        if(s.DiemTB is <0 or >10) throw new BusinessRuleException("Điểm trung bình phải thuộc khoảng 0 đến 10.");
        s.Email=string.IsNullOrWhiteSpace(s.Email)?null:s.Email.Trim();
        if(s.Email is { } email) { try { if(new MailAddress(email).Address!=email) throw new BusinessRuleException("Email không đúng định dạng."); } catch(FormatException){throw new BusinessRuleException("Email không đúng định dạng.");} }
    }
    private static void ValidateGrade(decimal? grade) { if(grade is <0 or >10) throw new BusinessRuleException("Điểm phải nằm trong khoảng 0 đến 10."); }
    private static string Required(string? text,int max,string message)
    {
        text=text?.Trim(); if(string.IsNullOrWhiteSpace(text)) throw new BusinessRuleException(message);
        if(text.Length>max) throw new BusinessRuleException($"Giá trị tối đa {max} ký tự."); return text;
    }
    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch(DalException ex) when(ex.ErrorNumber is 50001 or 50002 or 50011 or 50012 or 50013 or 50021 or 50031 or 50041 or 50042 or 50043 or 50051 or 50052 or 2601 or 2627 or 547)
        { throw new BusinessRuleException(ex.UserMessage); }
        catch(DalException ex) { throw new ApplicationDataException(ex.UserMessage,ex.TechnicalDetails,ex); }
        catch(System.Data.SqlClient.SqlException ex) { var dal=SqlErrorTranslator.Translate(ex); throw new BusinessRuleException(dal.UserMessage); }
        catch(ArgumentOutOfRangeException ex) { throw new BusinessRuleException(ex.Message); }
        catch(ArgumentException ex) { throw new BusinessRuleException(ex.Message); }
    }
}
