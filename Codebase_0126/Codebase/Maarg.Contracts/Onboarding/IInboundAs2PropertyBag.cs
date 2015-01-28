
namespace Maarg.Contracts
{
    public interface IInboundAs2PropertyBag : IBasePropertyBag
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "MDN", Justification = "Correct spelling is used.")]
        bool GenerateMDN { get; set; }
    }
}
