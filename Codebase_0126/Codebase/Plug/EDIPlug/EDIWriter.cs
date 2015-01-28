using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml;
using System.Collections;
using System.IO;

namespace Maarg.Fatpipe.EDIPlug
{
    public class EDIWriter
    {
        IDocumentPlug plug;
        public XmlDocument initialize(string xmlContent, IDocumentPlug plug)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            this.plug = plug;
            return doc;
        }


        public void printErrorMessages()
        {
            if (plug.Error.Count > 0)
            {
                Console.WriteLine("Following errors occured:");
                foreach (string error in plug.Error)
                    Console.WriteLine(error);
            }
            else
            {
                Console.WriteLine("Successfully Validated");
            }
        }


        private static string retreiveFullName(XmlNode node)
        {
            string fullName = node.Name;
            while (node.ParentNode.NodeType != XmlNodeType.Document)
            {
                node = node.ParentNode;
                fullName = node.Name + "/" + fullName;

            }

            return fullName;
        }


        public void validation(XmlNodeList nodes, IPluglet root)
        {
           foreach (XmlNode node in nodes)
           {
               // Console.WriteLine("Validating node " + node.Name);
                if (!node.FirstChild.HasChildNodes)
                 continue;

                IPluglet result = extractPluglet(root, node, this.plug);
                validateChild(result.Children, node.ChildNodes,result.PlugletType, 0, false);
                validation(node.ChildNodes, root);
           }
        }

        public void validateChild(IList<IPluglet> children, XmlNodeList nodes, PlugletType type, int index, bool flag)
        {
            
                for (int i = 0; i < children.Count; i++)
                {
                    if (index >= nodes.Count || !children[i].Name.Equals(nodes[index].Name))
                    {
                        if (children[i].IsMandatory&&!flag)
                            plug.Error.Add("Missing Mandatory Element " + children[i].Name+" in "+children[i].Parent.Name);

                            continue;
                    }

                    index++;
                }

                if (nodes.Count > index)
                {
                    if (type == PlugletType.Loop)
                        validateChild(children, nodes, type, index, true);
                    else
                        plug.Error.Add("Extra Element "+nodes[index].Name);

                }
        }

   
        public static IPluglet extractPluglet(IPluglet root, XmlNode node, IDocumentPlug plug)
        {
                string result = retreiveFullName(node);
                string[] nodeNames = result.Split('/');
                IPluglet current = root;    
            if (current.Name.Equals(nodeNames[0]))
                {
                    for (int i = 1; i < nodeNames.Length; i++)
                    {
                        current = findPlugletByName(current.Children, nodeNames[i]);
                        if (current == null)
                            plug.Error.Add("Could not find Element " + nodeNames[i]);
                    }
                }
                else
                {
                    plug.Error.Add("Root Element does not match");
                }
                return current;

        }

        public static IPluglet findPlugletByName(IList<IPluglet> children, string name)
        {
            foreach (IPluglet child in children)
            {
                if (child == null)
                    continue;
                if (child.Name.Equals(name))
                {
                    return child;
                }
            }
            return null;
        }


        public IDocumentFragment createDocumentFragment(IPluglet root, XmlNodeList nodes)
        {
            DocumentFragment docFrag = new DocumentFragment();
            IPluglet result = extractPluglet(root, nodes[0], this.plug);
            docFrag.Pluglet = result;
            docFrag.Parent = null;
            createDocumentFragment(root, nodes[0], docFrag);
            return docFrag;
        }

        public void createDocumentFragment(IPluglet root, XmlNode node, DocumentFragment docFrag)
        {
                
            foreach (XmlNode temp in node.ChildNodes)
            {
                if (!temp.HasChildNodes)
                    continue;
                IPluglet result = extractPluglet(root, temp, this.plug);
                DocumentFragment current = new DocumentFragment();
                current.Pluglet = result;
                current.Parent = docFrag;
                    
                if (!temp.FirstChild.HasChildNodes)
                    current.Value = temp.FirstChild.Value;
                   
                DocumentFragmentExtensions.AddDocumentFragment(docFrag, current);
                createDocumentFragment(root, temp, docFrag);
            }
        }
    }
}
