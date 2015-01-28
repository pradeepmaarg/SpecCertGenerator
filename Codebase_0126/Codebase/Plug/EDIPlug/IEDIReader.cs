using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using Maarg.FatpipeAPI;
using Maarg.Contracts;

namespace Maarg.Fatpipe.EDIPlug
{
    public interface IEDIReader
    {
        InterchangeErrors Errors { get; }

        Delimiters EDIDelimiters { get; }

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        bool Initialize(Stream stream);

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fatpipeManager"></param>
        /// <returns></returns>
        bool Initialize(Stream stream, IFatpipeManager fatpipeManager);

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fatpipeManager"></param>
        /// <param name="documentPlug"></param>
        /// <returns></returns>
        bool Initialize(Stream stream, IFatpipeManager fatpipeManager, IDocumentPlug documentPlug);

        /// <summary>
        /// Return next EDI transaction set.
        /// IFatpipeDocument will contain ISA, GA and 1 ST segment
        /// </summary>
        /// <returns></returns>
        IFatpipeDocument GetNextTransactionSet();

        /// <summary>
        /// Return GS 3rd data segment value
        /// </summary>
        /// <returns></returns>
        string GetSenderId();
    }
}
