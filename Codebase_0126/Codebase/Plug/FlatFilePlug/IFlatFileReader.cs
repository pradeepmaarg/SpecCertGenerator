using System.IO;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Contracts;

namespace Maarg.Fatpipe.FlatFilePlug
{
    public interface IFlatFileReader
    {
        InterchangeErrors Errors { get; }

        /// <summary>
        /// Read flat file and construct IFatpipeDocument
        /// IFatpipeDocument will NOT contain ISA and GA segments as it's not 
        /// present in case of flat file.
        /// Since ISA segment is missing and there is no speific ST segment
        /// DocumentPlug is mandatory parameter.
        /// </summary>
        /// <returns></returns>
        IFatpipeDocument ReadFile(Stream flatFileStream, IDocumentPlug documentPlug);
    }
}