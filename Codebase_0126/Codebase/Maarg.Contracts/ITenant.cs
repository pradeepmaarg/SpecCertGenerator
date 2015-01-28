using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Maarg.Contracts
{
    /// <summary>
    /// This interface represents a Maarg tenant.
    /// The tenat it the root level entity in a Maarg deployment.
    /// It is represented by a tenant trading partner and
    /// the list of trading partners that the tenant exchanges messages with.
    /// </summary>
    public interface ITenant : IIdentifier
    {
        /// <summary>
        /// Gets or sets the tenant partner.
        /// </summary>
        IPartner TenantPartner { get; set; }

        /// <summary>
        /// Gets the tenant's <see cref="IPartner"/> list of trading <see cref="IPartner"/>s.
        /// </summary>
        Collection<IPartner> TradingPartners { get; }

        /// <summary>
        /// Returns the list of <see cref="IFtpConnectorConfiguration"/> for the tenant.
        /// </summary>
        /// <returns>The list of <see cref="IFtpConnectorConfiguration"/> for the tenant.</returns>
        List<IFtpConnectorConfiguration> ListTenantFtpConnectorConfigurations();

        /// <summary>
        /// Returns the list of <see cref="IInboxConnectorConfiguration"/> for the tenant.
        /// </summary>
        /// <returns>The list of <see cref="IInboxConnectorConfiguration"/> for the tenant.</returns>
        List<IInboxConnectorConfiguration> ListTenantInboxConnectorConfigurations();
    }
}
