using System.Data;
using System.Net.Mail;
using Lab12.DAL;
using Lab12.DTO;

namespace Lab12.BUS;

public sealed class StudentService
{
    private readonly StudentRepository _repository = new();

    public DataTable Search(string? keyword, string? facultyCode, bool includeInactive) =>
        RunDal(() => _repository.Search(keyword, facultyCode, includeInactive));

    public int Add(StudentDto student)
    {
        Validate(student, requireCode: true);
        return RunDal(() => _repository.Insert(student));
    }

    public int Update(StudentDto student)
    {
        Validate(student, requireCode: true);
        return RunDal(() => _repository.Update(student));
    }

    public int SoftDelete(string code)
    {
        ValidateCode(code);
        return RunDal(() => _repository.SoftDelete(code.Trim()));
    }

    public int Restore(string code)
    {
        ValidateCode(code);
        return RunDal(() => _repository.Restore(code.Trim()));
    }

    private static void Validate(StudentDto student, bool requireCode)
    {
        ArgumentNullException.ThrowIfNull(student);
        student.MaSV = student.MaSV.Trim();
        student.HoTen = student.HoTen.Trim();
        student.MaKhoa = student.MaKhoa.Trim();
        student.Email = string.IsNullOrWhiteSpace(student.Email) ? null : student.Email.Trim();
        ValidateCode(student.MaSV, requireCode);
        if (student.MaSV.Length > 15) throw new BusinessRuleException("Mã sinh viên tối đa 15 ký tự.");
        if (student.HoTen.Length == 0) throw new BusinessRuleException("Họ tên không được để trống.");
        if (student.HoTen.Length > 100) throw new BusinessRuleException("Họ tên tối đa 100 ký tự.");
        if (student.NgaySinh.Date > DateTime.Today.AddYears(-15)) throw new BusinessRuleException("Sinh viên phải đủ 15 tuổi.");
        if (student.NgaySinh.Date < DateTime.Today.AddYears(-120)) throw new BusinessRuleException("Ngày sinh chưa hợp lệ.");
        if (string.IsNullOrWhiteSpace(student.MaKhoa) || student.MaKhoa.Length > 10) throw new BusinessRuleException("Hãy chọn khoa hợp lệ.");
        if (student.DiemTB is < 0 or > 10) throw new BusinessRuleException("Điểm trung bình phải thuộc khoảng 0 đến 10.");
        if (student.Email is { } email)
        {
            if (email.Length > 100) throw new BusinessRuleException("Email tối đa 100 ký tự.");
            try
            {
                var parsed = new MailAddress(email);
                if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException("Email không đúng định dạng.");
            }
            catch (FormatException) { throw new BusinessRuleException("Email không đúng định dạng."); }
        }
    }

    private static void ValidateCode(string? code, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(code)) throw new BusinessRuleException("Mã sinh viên không được để trống.");
        if (!string.IsNullOrWhiteSpace(code) && code.Trim().Length > 15)
            throw new BusinessRuleException("Mã sinh viên tối đa 15 ký tự.");
    }

    private static T RunDal<T>(Func<T> action)
    {
        try { return action(); }
        catch (DalException ex) when (ex.ErrorNumber is 50001 or 50002 or 50011 or 50012 or 50013 or 50021 or 50031 or 2601 or 2627 or 547)
        {
            throw new BusinessRuleException(ex.UserMessage);
        }
        catch (DalException ex)
        {
            throw new ApplicationDataException(ex.UserMessage, ex.TechnicalDetails, ex);
        }
    }
}
