using System.IO;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Contracts;

namespace Maarg.Fatpipe.XmlFilePlug
{
    public interface IXmlFileReader
    {
        InterchangeErrors Errors { get; }

        /// <summary>
        /// Read xml file and construct IFatpipeDocument.
        /// Xml file reader will traverse Xml files and for each element
        /// match it with current pluglet. If match fails then it tries to 
        /// find matching pluglet (similar to X12).
        /// </summary>
        /// <returns></returns>
        IFatpipeDocument ReadFile(Stream flatFileStream, IDocumentPlug documentPlug);
    }
}