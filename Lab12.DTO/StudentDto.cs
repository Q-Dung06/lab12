namespace Lab12.DTO;

public sealed class StudentDto
{
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public DateTime NgaySinh { get; set; }
    public bool GioiTinh { get; set; }
    public string MaKhoa { get; set; } = "";
    public decimal DiemTB { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[]? RowVersion { get; set; }
}

public sealed record FacultyDto(string MaKhoa, string TenKhoa, int SoSinhVien = 0);
