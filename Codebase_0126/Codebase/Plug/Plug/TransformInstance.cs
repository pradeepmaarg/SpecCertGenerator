using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class TransformInstance
    {
        static XElement input;

        static XDocument inputDoc;

        public static ITransformPlug plug;

        static XElement targetXML;

        static XName superParent;

        static XElement parent;

        public static Dictionary<string, string> mappedElements;

        public static string TransformSourceToTargetXml(ITransformPlug transPlug, string inputText)
        {

            mappedElements = new Dictionary<string, string>();

            input = XElement.Parse(inputText);

            inputDoc = XDocument.Parse(inputText);

            plug = transPlug;

            targetXML = null;

            bool flag = false;

            //handleAutoField(flag);

            targetXML = null;
            
            foreach (var node in input.Descendants())
            {
                flag = false;
                
                if (!node.HasElements)
                {
                    if (node.PreviousNode == null && (node.Parent.Parent.Parent.Parent == null || node.Parent.Parent.Parent.Parent.Parent == null))
                        flag = true;
                    else
                    {
                        if(node.PreviousNode !=null)
                            flag = false;
                        else
                        {
                            XElement tempNode = node;
                            while (tempNode != null)
                            {
                                if (tempNode.Parent.PreviousNode == null)
                                {
                                    if (tempNode.Parent.Parent.Parent.Parent == null || tempNode.Parent.Parent.Parent.Parent.Parent == null)
                                    {
                                        flag = true;
                                        break;
                                    }
                                    tempNode = tempNode.Parent;

                                }
                                else
                                {
                                    if (tempNode.Parent.Parent.Parent.Parent == null || tempNode.Parent.Parent.Parent.Parent.Parent == null)
                                    {
                                        flag = true;
                                    }
                                    else
                                    {
                                        flag = false;
                                    }
                                    break;
                                }
                                
                            }
                        }
                    }
                    
                    string current = string.Empty;
                    var nodes = node.AncestorsAndSelf();
                    foreach (var element in nodes)
                    {
                        current = "->" + element.Name + current;
                    }

                    string mappedElement = GetMapping(current);

                    if (!string.IsNullOrEmpty(mappedElement))
                    {
                        try
                        {
                            mappedElements.Add(current, mappedElement);
                        }
                        catch (ArgumentException e)
                        {
                        }
                        CreateXML(node, flag);
                       
                        //CreateXML(node.Value, mappedElement, flag);
                    }
                }
            }
            //mapFormula();
            //targetXML.Add(parent);
            return targetXML.ToString();
        }

        //function to handle auto field
       /* private static void handleAutoField(bool flag)
        {
            foreach (ITransformGroup group in plug.Facets)
            {
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Source.ReferenceType == ReferenceType.Literal)
                    {
                        CreateXML(link.Source.Name, link.Target.Name, flag);
                    }
                }
            }
        }
        */



        public static string GetMapping(string sourceName)
        {
            foreach (ITransformGroup group in plug.Facets)
            {
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Source.ReferenceType == ReferenceType.Formula || link.Target.ReferenceType == ReferenceType.Formula)
                    {
                        continue;

                    }

                    if (link.Source.Name.Equals(sourceName))
                    {
                        return link.Target.Name;

                    }
                }
            }

            return null;
        }

       /* public static void mapFormula(bool flag)
        {
            string result = string.Empty;
            foreach (ITransformGroup group in plug.Facets)
            {
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Ignore)
                        continue;
                    if (link.Source.ReferenceType == ReferenceType.Formula)
                    {
                        result = evaluateFunctoid(link.Source.Name);

                    }
                    else
                    {
                        result = extractParameterValues(link.Source.Name);
                    }

                    if (link.Target.ReferenceType == ReferenceType.Formula)
                    {
                        updateFormulaParameters(extractFormulaByNumber(link.Target.Name), result);
                    }

                    if (link.Target.ReferenceType == ReferenceType.Document && link.Source.ReferenceType == ReferenceType.Formula)
                    {
                        if (!string.IsNullOrEmpty(result))

                            CreateXML(result, link.Target.Name, flag);
                    }

                }
            }

        }
        */


        private static IFormula extractFormulaByNumber(string formulaId)
        {
            foreach (ITransformGroup group in plug.Facets)
                foreach (IFormula formula in group.Formulas)
                    if (formula.Name.Equals(formulaId))
                        return formula;
            return null;
        }

        private static void updateFormulaParameters(IFormula formula, string value)
        {
            for (int i = 0; i < formula.Parameters.Count; i++)
            {
                if (formula.Parameters[i].Reference.ReferenceType == ReferenceType.Document)
                {
                    formula.Parameters[i].Reference.Name = value;
                    formula.Parameters[i].Reference.ReferenceType = ReferenceType.Literal;
                }
            }
        }

        private static string evaluateFunctoid(string linkId)
        {
            string parameters = string.Empty;

            try
            {
                foreach (ITransformGroup group in plug.Facets)
                {
                    foreach (IFormula formula in group.Formulas)
                    {
                        if (formula.Name.Equals(linkId))
                        {
                            Console.WriteLine(formula.FormulaType);
                            foreach (IParameter param in formula.Parameters)
                            {
                                if (param.Reference.ReferenceType == ReferenceType.Literal)
                                {
                                    parameters += param.Reference.Name + "/";
                                    continue;
                                }
                                parameters += extractParameterValues(extractParameterNames(param.Reference.Name)) + "/";
                            }

                            return (ApplyFormula(formula.FormulaType, parameters.Substring(0, parameters.Length - 1).Split('/')));

                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        private static string ApplyFormula(FormulaType type, string[] parameters)
        {
            string result = string.Empty;

            switch (type)
            {
                case FormulaType.Equality: result = FormulaFactory.Equality(parameters).ToString();
                    break;
                case FormulaType.LogicalOr: result = FormulaFactory.LogicalOR(parameters).ToString();
                    break;
                case FormulaType.ValueMapping: result = FormulaFactory.ValueMapping(parameters).ToString();
                    break;
                case FormulaType.Copy: result = parameters[0];
                    break;
            }
            return result;
        }

        private static string extractParameterValues(string paramName)
        {
            paramName = paramName.Replace("->", "/");
            paramName = "/" + paramName;
            var found = inputDoc.XPathEvaluate(paramName) as IEnumerable<object>;
            foreach (var obj in found)
            {
                XElement temp = XElement.Parse(obj.ToString());
                if (!temp.HasElements)
                    return temp.Value;
            }
            return null;
        }

        private static string extractParameterNames(string parameterValue)
        {
            foreach (ITransformGroup group in plug.Facets)
            {
                foreach (ITransformLink link in group.Links)
                    if (link.Name.Equals(parameterValue))
                        return link.Source.Name;
            }
            return null;
        }



       /* private static void CreateXML(string result, string mappedElement, bool flag)
        {
            mappedElement = mappedElement.Replace("->", "/");
            //mappedElement = "/" + mappedElement;
            if (targetXML == null)
            {
                string[] root = mappedElement.Split('/');
                targetXML = new XElement(root[1]);
            }

            BuildTree(ref targetXML, mappedElement.Substring(mappedElement.IndexOf('/', 1)),
                mappedElement.Substring(mappedElement.LastIndexOf('/') + 1), result, flag);
        }
        */

        private static void CreateXML(XElement inner, bool flag)
        {
            XElement current = inner;

            if (inner != null && flag)
            {               
                foreach (var node in inner.Ancestors())
                {
                    if (node.Parent.Parent.Parent == null || node.Parent.Parent.Parent.Parent == null)
                                    
                    {
                        parent = new XElement(node.Name);
                        Console.WriteLine(parent);
                        break;
                    }
                }
            }
            
        
            if (!flag)
            {
                string name = inner.Parent.Name.ToString();
                XElement immediate = null;
                XElement tempNode = inner;
                string element = string.Empty;
                while (true)
                {
                    foreach (var node in parent.DescendantsAndSelf())
                    {
                        if (node.Name.Equals(tempNode.Parent.Name))
                            immediate = node;
                    }

                    if (immediate != null)
                    {
                        if (!string.IsNullOrEmpty(element))
                        {
                            tempNode = XElement.Parse(element);
                            foreach (var node in tempNode.DescendantsAndSelf())
                            {
                                if (!node.HasElements)
                                {
                                    node.Add(inner);
                                    break;
                                }
                            }
                        }
                        immediate.Add(tempNode);
                        return;
                    }
                    tempNode = tempNode.Parent;
                    element = "<"+tempNode.Name.ToString()+">"+element+"</"+tempNode.Name.ToString()+">";

                }
            }
                string temp = null;
                while (inner.Name != parent.Name)
                {
                    if (temp == null)
                    {
                        temp = new XElement(inner).ToString();
                    }
                    else
                    {
                        temp = "<" + inner.Name.ToString() + ">" + temp + "</" + inner.Name.ToString() + ">";
                    }
                    inner = inner.Parent;
                }
                if (temp == null)
                    parent.Add(inner);
                else
                {
                    parent.Add(XElement.Parse(temp));
                }
                getSuperParent(current);
                print();
                 
        }

        private static void print()
        {
            Console.WriteLine(parent);
           
            
                
                    //current = targetXML.Element(node.Name);
                    foreach (var tempNode in targetXML.DescendantsAndSelf())
                    {
                        if (tempNode.Name.Equals(superParent))
                        {
                            tempNode.Add(parent);
                            break;
                        }
                    }
                    
           
                    
           // current.Add(parent);
            Console.WriteLine(targetXML.ToString());
        }

        private static void getSuperParent(XElement node)
        {
            XElement current;
            string element = string.Empty;
            Console.WriteLine(parent.Name);
            while (node.Name != parent.Name)
            {
                node = node.Parent;
            }
                XName name = node.Parent.Name;
            while (node.Parent!=null)
            {
                if (targetXML != null && node.Parent.Parent == null)
                    break;
                element = "<" + node.Parent.Name + ">" + element + "</" + node.Parent.Name.ToString() + ">";
                node = node.Parent;
            }
            Console.WriteLine(element);
            current = XElement.Parse(element);
            if (targetXML == null)
            {
                targetXML = new XElement(current);
            }
            if (targetXML.Element(name) != null)
            {
                superParent = targetXML.Element(name).Name;
            }

             else
                {

                    //superParent = current;
                bool tempFlag = false;
                foreach(var tempNode in targetXML.DescendantsAndSelf())
                {
                    if (tempNode.Name.Equals(name))
                    {
                        tempFlag = true;
                        break;
                    }
                }
                if(!tempFlag)
                {
                    targetXML.Add(current);
                }
                    foreach (var tempNode in targetXML.DescendantsAndSelf())
                    {
                        if (tempNode.Name.Equals(name))
                        {
                            superParent = tempNode.Name;
                            break;
                        }
                    }
                }
        }

      

   /*     private static XElement BuildTree(ref XElement targetXML, string innerParentPath, string name, object value, bool flag)
        {
            List<string> nodes = innerParentPath.Split('/').ToList();
            string str = string.Empty;
            XElement prevInner = null;

            if (nodes.Count != 2)
            {
                var temp = new List<string>(nodes);
                temp.RemoveRange(nodes.Count - 1, 1);
                string[] sa = temp.ToArray();
                str = string.Join("/", sa);
                prevInner = BuildTree(ref targetXML, str, name, value, flag);
            }

            else
            {
                prevInner = targetXML;
            }

           
            XElement inner = prevInner.Element(nodes[nodes.Count - 1]);

            if (inner == null || !inner.HasElements)
            {
               if(inner!=null)
                CreateXML(inner, flag);

                if (nodes[nodes.Count - 1] == name)
                {
                    prevInner.Add(new XElement(name, value));
                }

                else
                {                    
                        inner = new XElement(nodes[nodes.Count - 1]);
                        prevInner.Add(inner);
                }
            }

            return inner;
        }
    */
    }
}
