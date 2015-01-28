using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public static class PlugToXmlSchema
    {
        public static bool SerializeToStream(IDocumentPlug plug, Stream outputStream)
        {
            bool result = true;
            XmlSchema schema = SerializeToXSD(plug);
            schema.Write(outputStream);
            return result;
        }


        public static XmlSchema SerializeToXSD(IDocumentPlug plug)
        {
            IPluglet pluglet = plug.RootPluglet;
            if (pluglet == null)
                return null;

            XmlSchema schema = new XmlSchema();
            XmlSchemaElement rootElement = SerializeToXmlSchemaElement(pluglet);
            schema.Items.Add(rootElement);
            return schema;
        }

        private static XmlSchemaElement SerializeToXmlSchemaElement(IPluglet pluglet)
        {
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = pluglet.Name;
            SetMinAndMaxOccurs(pluglet, element);
            SetDocumentation(pluglet, element);

            //Generate complex type for every node other than data
            if (pluglet.PlugletType != PlugletType.Data && pluglet.Children != null && pluglet.Children.Count > 0)
            {
                XmlSchemaComplexType complexType = new XmlSchemaComplexType();
                element.SchemaType = complexType;
                XmlSchemaSequence sequence = new XmlSchemaSequence();
                complexType.Particle = sequence;
                foreach (IPluglet child in pluglet.Children)
                {
                    XmlSchemaElement childElement = SerializeToXmlSchemaElement(child);
                    sequence.Items.Add(childElement);
                }

            }

            //generate SimpleType
            else
            {
                XmlSchemaSimpleType simpleType = new XmlSchemaSimpleType();
                element.SchemaType = simpleType;
                XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
                restriction.BaseTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
                simpleType.Content = restriction;
                SetAppInfoAndDataType(pluglet, element);
            }

            return element;
        }

        private static void SetMinAndMaxOccurs(IPluglet pluglet, XmlSchemaElement element)
        {
            if (pluglet.RepetitionInfo.MinOccurs != 1)
            {
                element.MinOccurs = pluglet.RepetitionInfo.MinOccurs;
            }

            int maxOccurs = pluglet.RepetitionInfo.MaxOccurs;
            if (maxOccurs < 0)
            {
                element.MaxOccursString = "unbounded";
            }

            else if (maxOccurs != 1)
            {
                element.MaxOccursString = maxOccurs.ToString();
            }
        }

        private static void SetAppInfoAndDataType(IPluglet pluglet, XmlSchemaElement element)
        {
            X12BaseDataType dataType = pluglet.DataType;

            if (dataType != null && !string.IsNullOrEmpty(dataType.Name))
            {
                XmlSchemaAppInfo appInfo = new XmlSchemaAppInfo();
                
                XmlDocument doc = new XmlDocument();
                XmlElement elem = doc.CreateElement("STD_Info");
                elem.SetAttribute("DataType", dataType.Name);

                if (!string.IsNullOrEmpty(pluglet.DEStandard))
                {
                    elem.SetAttribute("Name", pluglet.DEStandard);
                }

                if (!string.IsNullOrEmpty(pluglet.DENumber))
                {
                    elem.SetAttribute("Number", pluglet.DENumber);
                }

                if (dataType.MaxLength > 0)
                {
                    elem.SetAttribute("MaximumLength", dataType.MaxLength.ToString());
                }

                XmlNode[] nodeArray = new XmlNode[1] { elem };
                appInfo.Markup = nodeArray;

                if (element.Annotation != null)
                {
                    element.Annotation.Items.Add(appInfo);
                }

                else
                {
                    XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
                    annotation.Items.Add(appInfo);
                    element.Annotation = annotation;
                }

                if (string.Equals(dataType.Name, X12DataTypeFactory.IDNew_DataTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    PopulateEnumerationValues(pluglet.DataType as X12_IdDataType, element);
                }
            }
        }


        private static void PopulateEnumerationValues(X12_IdDataType idType, XmlSchemaElement element)
        {
            if (idType == null || idType.AllowedValues == null || idType.AllowedValues.Count == 0) return;

            XmlSchemaSimpleType simpleType = element.SchemaType as XmlSchemaSimpleType;
            XmlSchemaSimpleTypeRestriction restriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;
            
            foreach (KeyValuePair<string, string> kvp in idType.AllowedValues)
            {
                XmlSchemaEnumerationFacet facet = new XmlSchemaEnumerationFacet();
                facet.Value = kvp.Key;
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
                    XmlSchemaDocumentation doc = new XmlSchemaDocumentation();
                    doc.Markup = TextToNodeArray(kvp.Value);
                    annotation.Items.Add(doc);
                    facet.Annotation = annotation;
                }

                restriction.Facets.Add(facet);
            }
        }

        private static void SetDocumentation(IPluglet pluglet, XmlSchemaElement element)
        {
            if (!string.IsNullOrEmpty(pluglet.Definition))
            {
                XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
                XmlSchemaDocumentation doc = new XmlSchemaDocumentation();
                doc.Markup = TextToNodeArray(pluglet.Definition);
                annotation.Items.Add(doc);
                element.Annotation = annotation;
            }
        }

        private static XmlNode[] TextToNodeArray(string text)
        {
            XmlDocument doc = new XmlDocument();
            return new XmlNode[1] { doc.CreateTextNode(text) };
        }
    }
}
