
namespace Maarg.Contracts
{
    public interface IPartnerAccount : IIdentifier
    {
        string Email { get; set; }
        string PartnerIdentifier { get; set; }
        string Role { get; set; }
        bool IsFirstTimeLogin { get; set; }
    }
}
