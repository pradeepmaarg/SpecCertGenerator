using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public interface IPlugDefinition
    {
        ITransformPlug TranformPlug { get; set; }
        DateTime ValidFrom { get; set; }
        DateTime ValidTo { get; set; }
        string DocType { get; set; }
    }

    public class PlugDefinition: IPlugDefinition
    {
        ITransformPlug transformPlug;
        DateTime validFrom;
        DateTime validTo;
        string docType;

        public PlugDefinition(ITransformPlug plug, DateTime start, DateTime end)
        {
            this.transformPlug = plug;
            this.validFrom = start;
            this.validTo = end;
        }

        #region Properties
        
        public string DocType
        {
            get { return (this.transformPlug != null && this.transformPlug.SourceDocument != null) ? this.transformPlug.SourceDocument.Name : string.Empty; }
            set { this.docType = value;}
        }

        public ITransformPlug TranformPlug
        {
            get { return this.transformPlug; }
            set { this.transformPlug = value; }
        }

        public DateTime ValidFrom
        {
            get { return this.validFrom; }
            set { this.validFrom = value; }
        }

        public DateTime ValidTo
        {
            get { return this.validTo; }
            set { this.validTo = value; }
        }

        #endregion Properties

    }
}
