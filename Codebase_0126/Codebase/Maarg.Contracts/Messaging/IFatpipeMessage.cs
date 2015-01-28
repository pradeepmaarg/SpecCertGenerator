using System.IO;

namespace Maarg.Contracts
{
    /// <summary>
    /// This is the basic unit of messaging. Fatpipe is all about movement and processing of this message
    /// The same data structure is used at different layers of the stack, including InboundConnectors,
    /// OutboundConnectors, B2BProtocolEngine
    /// 
    /// So, what does a Message contain? Simple
    /// 
    /// A body which is Stream of bytes
    /// A context which is a property bag
    /// 
    /// When a message is persisted, it is immutable. Clients can modify the in-memory data structure but
    /// once persisted it stays as is
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Fatpipe", Justification = "Correct spelling is used.")]
    public interface IFatpipeMessage
    {
        MessageHeader Header { get; set; }
        MessageBody Body { get; set; }
        MessageStatus Status { get; set; }
        IFatpipeMessage Clone(Stream stream);
    }

    /// <summary>
    /// A routable Bizpipe Message
    /// </summary>
    public interface IOutboundFatpipeMessage : IFatpipeMessage
    {
        RoutingInfo RoutingInfo { get; set; }
    }
}
