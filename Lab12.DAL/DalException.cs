namespace Lab12.DAL;

public sealed class DalException : Exception
{
    public int ErrorNumber { get; }
    public string UserMessage { get; }
    public string TechnicalDetails { get; }

    public DalException(int errorNumber, string userMessage, string technicalDetails, Exception? innerException = null)
        : base(userMessage, innerException)
    {
        ErrorNumber = errorNumber;
        UserMessage = userMessage;
        TechnicalDetails = technicalDetails;
    }
}
