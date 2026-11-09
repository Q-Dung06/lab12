using Lab12.DAL;

namespace Lab12.BUS;

public sealed class DatabaseSetupService
{
    private readonly DatabaseSetupRepository _repository = new();
    public int Prepare() => _repository.Prepare(Path.Combine(AppContext.BaseDirectory, "Database"));
}
