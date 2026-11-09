namespace Lab12.BUS;

public sealed class BusinessRuleException(string message) : Exception(message);

public sealed class ApplicationDataException(string message, string technicalDetails, Exception innerException)
    : Exception(message, innerException)
{
    public string TechnicalDetails { get; } = technicalDetails;
}
