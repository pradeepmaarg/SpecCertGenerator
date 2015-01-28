using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Maarg.Fatpipe.LoggingService;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public static class DocumentPlugExtensions
    {
        /// <summary>
        /// Merge documentPlug with newDocumentPlug and return mergedDocumentPlug.
        /// canonicalDocumentPlug is used to decide children order during merge process
        /// </summary>
        /// <param name="documentPlug"></param>
        /// <param name="newDocumentPlug"></param>
        /// <param name="canonicalDocumentPlug"></param>
        /// <returns></returns>
        public static IDocumentPlug Merge(IDocumentPlug documentPlug, IDocumentPlug newDocumentPlug, IDocumentPlug canonicalDocumentPlug)
        {
            if (documentPlug == null)
                throw new ArgumentNullException("documentPlug");
            if (newDocumentPlug == null)
                throw new ArgumentNullException("newDocumentPlug");
            if (canonicalDocumentPlug == null)
                throw new ArgumentNullException("canonicalDocumentPlug");

            DocumentPlug mergedDocumentPlug = new DocumentPlug(null, documentPlug.BusinessDomain);

            mergedDocumentPlug.RootPluglet = documentPlug.RootPluglet.Merge(newDocumentPlug.RootPluglet, canonicalDocumentPlug.RootPluglet);

            return mergedDocumentPlug;
        }
    }
}
