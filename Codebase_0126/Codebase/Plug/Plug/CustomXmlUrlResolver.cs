using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Maarg.Fatpipe.Plug.DataModel
{
    class CustomXmlUrlResolver : XmlUrlResolver
    {
        private const string MYRESOURCENAMESPACE = "Maarg.Fatpipe.Plug.DataModel.{0}";
        private Assembly resourceAssembly = null;

        public CustomXmlUrlResolver(Assembly resourceAssembly)
        {
            if (resourceAssembly == null)
                throw new ArgumentNullException("resourceAssembly must not be null");

            this.resourceAssembly = resourceAssembly;
        }

        override public object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (absoluteUri.IsFile)
            {
                string file = Path.GetFileName(absoluteUri.AbsolutePath);

                Stream stream = resourceAssembly.GetManifestResourceStream(
                   String.Format(MYRESOURCENAMESPACE, file));

                return stream;
            }

            return null;
        }
    } 
}
