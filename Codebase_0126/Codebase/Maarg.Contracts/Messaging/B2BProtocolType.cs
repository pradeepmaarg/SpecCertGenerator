namespace Maarg.Contracts
{
    public enum B2BProtocolType
    {
        AS2,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EDI", Justification = "Correct spelling is used.")]
        EDI,
        Xml,
        FlatFile,
        Custom
    }
}