using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    /// <summary>
    /// Maarg runs a multi-tenant service, providing B2B capabilities.
    /// Stated more explicitly, tenant is with reference to Maarg, and not for one partner in relation to other.
    /// It's very important have this clear in mind.
    /// 
    /// Thus, G* is a tenant
    /// 
    /// Partner is a business entity that has a relationship with Tenant. It wants to conduct B2B Commerce.
    /// Natually, Partner also has a relation with Maarg, but that is of type 'Cloud B2B service provider'. We are the pipe.
    /// 
    /// Partner contains multiple agreements, one for each Partner that it wants to conduct business with.
    /// </summary>
    public interface IPartner : IIdentifier
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        PartnerStatus Status { get; set; }

        /// <summary>
        /// Gets the partner identity based on specific B2B protocols like EDI, AS2. Multiple are possible.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Will not be used outside Maarg.")]
        List<string> ProtocolIdentifiers { get; } //this is the partner identity based on specific B2B protocols like EDI, AS2. Multiple are possible

        /// <summary>
        /// Gets the postal address of the partner.
        /// </summary>
        string PostalAddress { get; set; }

        /// <summary>
        /// Gets or sets the partner phone number.
        /// </summary>
        string Phone { get; set; }

        /// <summary>
        /// Gets or sets the primary email address.
        /// </summary>
        string EmailAddress { get; set; }

        /// <summary>
        /// Gets the list of documents.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Will not be used outside Maarg.")]
        List<string> Documents { get; }

        /// <summary>
        /// Gets or sets the default document endpoint.
        /// </summary>
        Uri DefaultDocumentEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPartner"/> connector configuration for transports like FTP, SMTP etc.
        /// </summary>
        IConnectorConfiguration ConnectorConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPartner"/> connector configuration for transports like FTP, SMTP etc.
        /// </summary>
        IConnectorConfiguration TargetConnectorConfiguration { get; set; }

        /// <summary>
        /// Total license users
        /// </summary>
        int TotalLicenseUsers { get; set; }

        /// <summary>
        /// Total users (already used the license)
        /// </summary>
        int TotalUsers { get; set; }
    }
}
