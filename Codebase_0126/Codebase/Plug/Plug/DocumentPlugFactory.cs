using System;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Collections;
using System.Reflection;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class DocumentPlugFactory
    {

        public static IDocumentPlug CreateDocumentPlugFromXmlSchema(string schemaPath, string targetNamespace, string name)
        {
            IDocumentPlug plug = XmlSchemaHelper.CreateDocumentPlug(schemaPath, targetNamespace, name);
            return plug;
        }

        public static IDocumentPlug CreateDocumentPlugFromXmlSchema(Stream schemaStream)
        {
            IDocumentPlug plug = XmlSchemaHelper.CreateDocumentPlug(schemaStream);
            return plug;
        }

        public static IDocumentPlug CreateDocumentPlugFromXmlSchema(string schemaPath)
        {
            Stream schemaStream = new FileStream(schemaPath, FileMode.Open, FileAccess.Read);
            IDocumentPlug plug = XmlSchemaHelper.CreateDocumentPlug(schemaStream);
            return plug;
        }

        public static IDocumentPlug CreateDocumentPlugFromXmlSchema(XmlSchemaCollection collection, string targetNamespace, string name)
        {
            IDocumentPlug plug = XmlSchemaHelper.CreateDocumentPlug(collection, targetNamespace, name);
            return plug;
        }

        public static IDocumentPlug CreateGComDocumentPlug()
        {
            string path = "GComPO.xsd";
            string targetNamespace = @"GCom_Schemas";
            string name = "GComPO";

            return DocumentPlugFactory.CreateDocumentPlugFromXmlSchema(path, targetNamespace, name);
        }

        /// <summary>
        /// This function should be used only for unit testing where we don't want to connect to Azure storage
        /// </summary>
        /// <param name="currentTransactionSetType"></param>
        /// <returns></returns>
        public static IDocumentPlug CreateEDIDocumentPlug(int currentTransactionSetType)
        {
            string path, targetNamespace, name;

            switch(currentTransactionSetType)
            {
                case 820:
                    path = "820-R1.xsd";
                    targetNamespace = @"urn:x12:schemas:005010X306:820R1:HealthInsuranceExchangeRelatedPayments";
                    name = "X12_005010X306_820R1";
                    break;

                case 850:
                    path = "GCommerce.EDI._00401._850.Schemas.Enriched_X12_00401_850";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/EDIFACT/2006/EnrichedMessageXML";
                    name = "X12EnrichedMessage";
                    break;

                case 277:
                    path = "X12_005010X214_277B3.xsd";
                    targetNamespace = @"urn:x12:schemas:005:010:277B3:HealthCareInformationStatusNotification";
                    name = "X12_005010X214_277B3";
                    break;

                case 810:
                    path = "X12_00501_810.xsd";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/X12/2006";
                    name = "X12_00501_810";
                    break;

                default:
                    throw new Exception(string.Format("{0} schema not found", currentTransactionSetType));
            }

            IDocumentPlug schemaPlug = DocumentPlugFactory.CreateDocumentPlugFromXmlSchema(path, targetNamespace, name);

            XElement schemaXml = schemaPlug.SerializeToXml();
            schemaXml.Save(string.Format("Schema_{0}.xml", currentTransactionSetType));

            return schemaPlug;
        }      
    }

    public class XmlSchemaHelper
    {
        private static void SchemaValidationEventHandler(object sender, ValidationEventArgs e)
        {
        }

        public static IDocumentPlug CreateDocumentPlug(string schemaPath, string targetNamespace, string name)
        {
            if (!File.Exists(schemaPath))
            {
                // Note: Control should reach here only for unit tests
                string rootPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\"),
                        @"sources\test\PlugTestHarness\output");

                schemaPath = Path.Combine(rootPath, schemaPath);
            }

            XmlSchemaCollection set = new XmlSchemaCollection();
            set.Add(targetNamespace, schemaPath);
            return CreateDocumentPlug(set, targetNamespace, name);
        }

        public static IDocumentPlug CreateDocumentPlug(Stream schemaStream)
        {
           XmlTextReader reader = new XmlTextReader(schemaStream);
           XmlSchema schema = XmlSchema.Read(reader, null); 
           XmlSchemaCollection set = new XmlSchemaCollection();
           set.Add(schema);
           string targetNamespace;
           string name = ExtractNamespaceandRootnodeName(schema, out targetNamespace);  

            return CreateDocumentPlug(set, targetNamespace, name);
        }

        public static IDocumentPlug CreateDocumentPlug(XmlSchema schema)
        {
            XmlSchemaCollection set = new XmlSchemaCollection();
            set.Add(schema);
            string targetNamespace;
            string name = ExtractNamespaceandRootnodeName(schema, out targetNamespace);

            return CreateDocumentPlug(set, targetNamespace, name);
        }

        public static IDocumentPlug CreateDocumentPlug(XmlSchema mainSchema, XmlSchemaCollection schemaCollection)
        {
            string targetNamespace;
            string name = ExtractNamespaceandRootnodeName(mainSchema, out targetNamespace);

            return CreateDocumentPlug(schemaCollection, targetNamespace, name);
        }

        public static IDocumentPlug CreateDocumentPlug(XmlSchemaCollection collection, string targetNamespace, string name)
        {
            IDocumentPlug plug = ParseSchema(new XmlQualifiedName(name, targetNamespace), collection[targetNamespace]);
            if (plug.Error.Count > 0)
            {
                throw new Exception("Error encountered in loading schema");
            }
            return plug;
        }


        private static string ExtractNamespaceandRootnodeName(XmlSchema schema, out string targetNamespace)
        {
            string rootNodeName = string.Empty;
            targetNamespace = schema.TargetNamespace;
            IEnumerator schemaItems = schema.Items.GetEnumerator();

            //This assumption is for WPC schemas 5/25/2013
            //First element in the schema is the root Node
            while (schemaItems.MoveNext())
            {
                if (schemaItems.Current is XmlSchemaElement)
                {
                    rootNodeName = ((XmlSchemaElement)schemaItems.Current).Name;
                    break;
                }
            }

            return rootNodeName;
        }

        private static IDocumentPlug ParseSchema(XmlQualifiedName rootElement, XmlSchema schema)
        {
            IDocumentPlug plug = new DocumentPlug();
            IPluglet root = null;
            if (schema == null)
            {
                plug.Error.Add("SchemaCode101ENullSchema");
                return plug;
            }

            // Extract the root element
            XmlSchemaElement elem = (XmlSchemaElement)schema.Elements[rootElement];

            if (elem != null)
            {
                root = ParseElement(null, elem, schema);
            }

            else
            {
                plug.Error.Add("SchemaCode102ENullRootElement");
            }

            plug.RootPluglet = root;
            return plug;
        }

        /*
            * This method is used to parse an element. An element declaration
            * looks like one of the following
            * 
            * <xs:element name="name" min="0" max="1" type="ns:type">
            * 
            * <xs:element ref="name" min="0" max="1">
            * 
            * <xs:element name="name" min="0" max="1">
            *  <xs:complexType>
            *    <xs:sequence>
            *       <xs:element name = ...>
            *       <xs:element name = ...>
            *       <xs:sequence min=0 max=1>
            *          <xs:element ...>
            *   </xs:sequence>
            *  </xs:complexType>
            * </xs:element>
            */
        private static IPluglet ParseElement(IPluglet parent, XmlSchemaElement elem, XmlSchema schema)
        {
            IPluglet ret = null;
            XmlSchemaElement refElem = elem;

            if (elem.Name == null)
            {
                refElem = (XmlSchemaElement)schema.Elements[elem.RefName];
            }

            //Console.WriteLine("ParseElement.. " + refElem.Name);
            Dictionary<string, string> pairs;
            string description = ReadDocumentationFromElement(elem, !(elem.ElementType is XmlSchemaComplexType), out pairs);
            if (string.IsNullOrEmpty(description) && string.IsNullOrEmpty(elem.Name))
            {
                description = ReadDocumentationFromElement(refElem, !(elem.ElementType is XmlSchemaComplexType), out pairs);
            }

            int maxOccurs = elem.MaxOccursString == "unbounded" ? -1 : (int)elem.MaxOccurs;
            //int maxOccurs = elem.MaxOccursString == "unbounded" || string.IsNullOrEmpty(elem.MaxOccursString) ? -1 : (int)elem.MaxOccurs;
            int minOccurs = (int)elem.MinOccurs;

            //Console.WriteLine("Parsing element " + elem.QualifiedName.Name);
            XmlSchemaComplexType elemType = elem.ElementType as XmlSchemaComplexType;
            if (elemType != null)
            {
                string name = elem.QualifiedName.Name;

                if (name.IndexOf("Loop", StringComparison.Ordinal) >= 0)
                {
                    ret = new Pluglet(name, description, PlugletType.Loop, parent, minOccurs, maxOccurs);
                }

                else if (name != "UNA" && name != "UNB" && name != "UNG")
                {
                    PlugletType type = PlugletType.Segment;
                    if (parent != null && parent.PlugletType == PlugletType.Segment && parent.Parent != null)
                    {
                        type = PlugletType.CompositeData;
                    }

                    //the only exception is for EDI documents where we have component fields, they begin with C0*
                    if (name.StartsWith("C0")) type = PlugletType.Data;

                    ret = new Pluglet(name, description, type, parent, minOccurs, maxOccurs);
                }
                else // In case of UNA, UNB and UNG elements just return (ignore those elements)
                    return null;

                XmlSchemaComplexType ct = (XmlSchemaComplexType)refElem.ElementType;
                ParseComplexType(ret, ct, schema);
            }

            /*
            * If elementType is simple, create a SimpleField and proceed
            */
            else if (elem.ElementType is XmlSchemaSimpleType || elem.ElementType is XmlSchemaDatatype)
            {
                string name = elem.QualifiedName.Name;
                ret = new Pluglet(name, description, PlugletType.Data, parent, minOccurs, maxOccurs);
            }

            if (ret.PlugletType == PlugletType.Data)
            {
                //This is old behavior for BTS schemas. Raj@5/25/2013
                //ret.DataType = DataTypeFactory.CreateDataTypeFromXmlSchema(elem, false);

                //Now we need to use WPC logic
                string dataTypeName, minL, maxL;
                dataTypeName = RetrieveDataTypeAndStampStandarDENumber(ret, elem, out minL, out maxL);
                if (string.IsNullOrWhiteSpace(dataTypeName))
                    ret.DataType = DataTypeFactory.CreateDataTypeFromXmlSchema2(elem, false);
                else
                    ret.DataType = DataTypeFactory.CreateDataTypeFromXmlSchema(elem, false, dataTypeName, minL, maxL);
            }

            return ret;
        }

        private static string RetrieveDataTypeAndStampStandarDENumber(IPluglet pluglet, XmlSchemaElement elem, out string minL, out string maxL)
        {
            string dataTypeName, standard, deNumber;
            dataTypeName = minL = maxL = standard = deNumber = string.Empty;
            Dictionary<string, string> pairs;
            ReadDocumentationFromElement(elem, true, out pairs);
            if (pairs != null)
            {
                pairs.TryGetValue("Name", out standard);
                pairs.TryGetValue("Number", out deNumber);
                pairs.TryGetValue("DataType", out dataTypeName);
                pairs.TryGetValue("MaximumLength", out maxL);
                pairs.TryGetValue("MinimumLength", out minL);
            }

            pluglet.DENumber = deNumber;
            pluglet.DEStandard = standard;

            return dataTypeName;
        }

        public static string ReadDocumentationFromEnumeration(XmlSchemaEnumerationFacet enumeration)
        {
            string documentation = string.Empty;
            XmlSchemaAnnotation annotation = enumeration.Annotation;
            if (annotation != null && annotation.Items != null)
            {
                XmlSchemaObjectCollection coll = annotation.Items;
                foreach (XmlSchemaObject obj in coll)
                {
                    if (obj is XmlSchemaDocumentation)
                    {
                        XmlSchemaDocumentation doc = (XmlSchemaDocumentation)obj;
                        XmlNode[] node = doc.Markup;
                        if (node != null)
                        {
                            for (int i = 0; i < node.Length; i++)
                            {
                                if (node[i] is XmlText)
                                {
                                    documentation = (node[i] as XmlText).InnerText;
                                }
                            }
                        }
                    }
                }
            }

            return documentation;
        }

        private static string ReadDocumentationFromElement(XmlSchemaElement elem, bool isField, out Dictionary<string, string> pairs)
        {
            pairs = new Dictionary<string, string>(5);
            XmlSchemaAnnotation annotation = elem.Annotation;
            string reference = string.Empty;
            if (annotation != null && annotation.Items != null)
            {
                XmlSchemaObjectCollection coll = annotation.Items;
                foreach (XmlSchemaObject obj in coll)
                {
                    if (obj is XmlSchemaAppInfo)
                    {
                        XmlSchemaAppInfo appInfo = (XmlSchemaAppInfo)obj;
                        XmlNode[] node = appInfo.Markup;
                        if (node != null)
                        {
                            for (int i = 0; i < node.Length; i++)
                            {
                                XmlElement element = node[i] as XmlElement;
                                if (element != null)
                                {
                                    if (string.Compare(element.LocalName, "STD_info", StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        string data = element.GetAttribute("Name");
                                        if (!string.IsNullOrEmpty(data)) pairs["Name"] = data;

                                        data = element.GetAttribute("Number");
                                        if (!string.IsNullOrEmpty(data)) pairs["Number"] = data;

                                        data = element.GetAttribute("DataType");
                                        if (!string.IsNullOrEmpty(data)) pairs["DataType"] = data;

                                        data = element.GetAttribute("MaximumLength");
                                        if (!string.IsNullOrEmpty(data)) pairs["MaximumLength"] = data;

                                        data = element.GetAttribute("MinimumLength");
                                        if (!string.IsNullOrEmpty(data)) pairs["MinimumLength"] = data;
                                    }
                                }

                                /*
                                if (node[i].NamespaceURI == "http://schemas.microsoft.com/BizTalk/2003"
                                    && ((node[i].LocalName == "recordInfo" && !isField) || (node[i].LocalName == "fieldInfo" && isField))
                                    && node[i] is XmlElement)
                                {
                                    reference = (node[i] as XmlElement).GetAttribute("notes");
                                    pairs["notes"] = reference;
                                    if (!string.IsNullOrEmpty(reference)) break;
                                }
                                 */
                            }
                        }
                    }

                    else if (obj is XmlSchemaDocumentation)
                    {
                        XmlSchemaDocumentation doc = (XmlSchemaDocumentation)obj;
                        XmlNode[] node = doc.Markup;
                        if (node != null)
                        {
                            for (int i = 0; i < node.Length; i++)
                            {
                                if (node[i] is XmlText)
                                {
                                    reference = (node[i] as XmlText).InnerText;
                                    pairs["Documentation"] = reference;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return reference;
        }

        private static IPluglet ParseComplexType(IPluglet parent, XmlSchemaComplexType ct, XmlSchema schema)
        {
            //Console.WriteLine("ParseComplexType.. " + ct.Name);
            IPluglet ret = null;
            XmlSchemaObjectCollection elementC = null;
            XmlSchemaObjectCollection attribC = ct.Attributes;

            //Schemas should have elements only. A migration tool is ready which can take
            //a schema containing attributes and elements and convert it to an elements only
            //schema
            if (attribC != null && attribC.Count > 0)
            {
                foreach (XmlSchemaAttribute attr in attribC)
                {
                    string attributeName = "{"+attr.Name+"}";
                    
                    int minOccurs = 0;
                    int maxOccurs = 1;
                    if(attr.Use.Equals(XmlSchemaUse.Required))
                        minOccurs = 1;
                    string description = string.Empty;
                    parent.Attributes.Add(new Pluglet(attributeName, description, PlugletType.Data, parent, minOccurs, maxOccurs));
                }
            }

            if (ct.ContentModel != null)
            {
                AppendSchemaError("SchemaCode105EUnexpectedContentModelFound");
            }

            if (ct.ContentModel == null && ct.Particle != null)
            {
                if (ct.Particle is XmlSchemaSequence)
                {
                    elementC = (ct.Particle as XmlSchemaGroupBase).Items;
                }
                else if (ct.Particle is XmlSchemaChoice)
                {
                    elementC = (ct.Particle as XmlSchemaChoice).Items;
                    //AppendSchemaError("SchemaCode128EXmlSchemaChoiceParentChoiceNotFound");
                }
                else if (ct.Particle is XmlSchemaAll)
                {
                    AppendSchemaError("SchemaCode126EXmlSchemaAllParentAllNotFound");
                }
                else
                {
                    AppendSchemaError("SchemaCode106EXmlSchemaGroupNotFound");
                }
            }

            if (elementC != null)
            {
                foreach (XmlSchemaObject o in elementC)
                {
                    ret = ParseElement(parent, (XmlSchemaElement)o, schema);
                }
            }

            return ret;
        }

        /*
            * ParseParticle
            *     
            * This method parses an XML particle.  A particle can be one of the following:
            *
            * XmlSchemaElement
            * XmlSchemaGroupBase - can be Sequence, Choice or All. 
            * XmlSchemaGroupRef
            */
        private static IPluglet ParseParticle(IPluglet parent, XmlSchemaParticle pa, XmlSchema schema)
        {
            //Console.WriteLine("ParseParticle.. " + pa.Id);
            IPluglet ret = null;

            if (pa is XmlSchemaElement)
            {
                ret = ParseElement(parent, (XmlSchemaElement)pa, schema);
            }

            else if (pa is XmlSchemaAny)
            {
                AppendSchemaError("SchemaCode107EXmlSchemaAnyFound");
            }

            else if (pa is XmlSchemaGroupBase)
            {
                if (pa is XmlSchemaAll)
                {
                    AppendSchemaError("SchemaCode108EXmlSchemaAllFound");
                }

                else if (pa is XmlSchemaSequence)
                {
                    AppendSchemaError("SchemaCode109EXmlSchemaSequenceFound");
                }

                else if (pa is XmlSchemaChoice)
                {
                    AppendSchemaError("SchemaCode110EXmlSchemaChoiceFound");
                }
            }

            else if (pa is XmlSchemaGroupRef)
            {
                ParseParticle(parent, (XmlSchemaParticle)((XmlSchemaGroupRef)pa).Particle,
                    schema);
            }

            return ret;
        }

        private static void AppendSchemaError(string error)
        {
            throw new Exception(error);
        }
    }
}
