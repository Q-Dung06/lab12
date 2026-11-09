using System.Data;
using Lab12.DAL;
using Lab12.DTO;

namespace Lab12.BUS;

public sealed class FacultyService
{
    private readonly FacultyRepository _repository = new();

    public DataTable GetAll() => RunDal(_repository.GetAll);

    public int Add(string? code, string? name)
    {
        var faculty = Validate(code, name);
        return RunDal(() => _repository.Insert(faculty));
    }

    public int Update(string? code, string? name)
    {
        var faculty = Validate(code, name);
        return RunDal(() => _repository.Update(faculty));
    }

    public int Delete(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleException("Chọn khoa cần xóa.");
        if (code.Trim().Length > 10) throw new BusinessRuleException("Mã khoa tối đa 10 ký tự.");
        return RunDal(() => _repository.Delete(code.Trim()));
    }

    private static FacultyDto Validate(string? code, string? name)
    {
        code = code?.Trim();
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleException("Mã khoa không được để trống.");
        if (code.Length > 10) throw new BusinessRuleException("Mã khoa tối đa 10 ký tự.");
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Tên khoa không được để trống.");
        if (name.Length > 100) throw new BusinessRuleException("Tên khoa tối đa 100 ký tự.");
        return new FacultyDto(code, name);
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
