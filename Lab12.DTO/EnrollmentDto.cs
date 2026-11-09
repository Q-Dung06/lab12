namespace Lab12.DTO;

public sealed record EnrollmentDto(string MaSV, string MaHP, int HocKy, decimal? Diem);

public sealed record GradeChange(string MaSV, string MaHP, decimal Diem);

public sealed record RollbackDemoResult(bool FailedAsExpected, bool StudentWasRemoved);
