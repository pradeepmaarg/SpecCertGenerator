using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class TransformPlugFactory
    {
        /// <summary>
        /// Construct TransformPlug (only Facets) fa from given File. 
        /// </summary>
        /// <param name="btmFileInfo"></param>
        /// <returns></returns>
        public static ITransformPlug CreateTransformPlugFromBTM(FileInfo btmFileInfo)
        {
            XElement btmRoot = XElement.Load(btmFileInfo.FullName);
            return MapperHelper.CreateTransformPlugFromBTM(btmRoot);
        }

        /// <summary>
        /// Construct TransformPlug (only Facets) fa from given stream. 
        /// </summary>
        /// <param name="btmStream"></param>
        /// <returns></returns>
        public static ITransformPlug CreateTransformPlugFromBTM(Stream btmStream)
        {
            XElement btmRoot = XElement.Load(btmStream);
            return MapperHelper.CreateTransformPlugFromBTM(btmRoot);
        }

        /// <summary>
        /// Construct ITranformPlug based on source and target plug
        /// </summary>
        /// <param name="sourcePlug"></param>
        /// <param name="targetPlug"></param>
        /// <returns></returns>
        public static ITransformPlug CreateTransformPlug(IDocumentPlug sourcePlug, IDocumentPlug targetPlug)
        {
            //TODO: Implement this - currently hard coded path
            string path = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\"),
                                @"sources\test\GCommerceSuperSpec\GCommerceSuperSpecInbound850FromBuyer\InboundPO.btm");
            FileInfo btmFileInfo = new FileInfo(path);

            XElement btmRoot = XElement.Load(btmFileInfo.FullName);
            return MapperHelper.CreateTransformPlugFromBTM(btmRoot, sourcePlug, targetPlug);
        }
    }

    class MapperHelper
    {
        public static ITransformPlug CreateTransformPlugFromBTM(XElement btmRoot)
        {
            return CreateTransformPlugFromBTM(btmRoot, null, null);
        }

        public static ITransformPlug CreateTransformPlugFromBTM(XElement btmRoot, IDocumentPlug sourcePlug, IDocumentPlug targetPlug)
        {

            string srcName, trgName;
            srcName = trgName = null;
            
            XElement e = GetFirstElement(btmRoot, "SrcTree");
            srcName = GetAttributeValue(e, "RootNode_Name");
            XElement reference = GetFirstElement(e, "Reference");
            string sourceLocation = null;
            if(reference != null)
                sourceLocation = GetAttributeValue(reference, "Location");

            e = GetFirstElement(btmRoot, "TrgTree");
            trgName = GetAttributeValue(e, "RootNode_Name");
            reference = GetFirstElement(e, "Reference");
            string targetLocation = null;
            if (reference != null)
                targetLocation = GetAttributeValue(reference, "Location");

            ITransformPlug plug = new TransformPlug(sourcePlug, targetPlug, null, sourceLocation, targetLocation);

            IEnumerable<XElement> pageList = btmRoot.Elements(XName.Get("Pages"));
            IEnumerable<XElement> pages = null;
            foreach (XElement p in pageList)
            {
                pages = p.Elements(XName.Get("Page"));
                break;
            }

            foreach (XElement page in pages)
            {
                ITransformGroup group = ParsePage(page);
                MarkForIgnore(group);
                plug.Facets.Add(group);
            }

            //PrintTransformPlug(plug);
            return plug;
        }

        static void PrintTransformPlug(ITransformPlug plug)
        {
            IList<ITransformGroup> groups = plug.Facets;
            foreach (ITransformGroup group in groups)
            {
                Console.WriteLine("Group name = " + group.Name);

                Console.WriteLine("\nPrinting links...");
                foreach (ITransformLink link in group.Links)
                {
                    //Console.WriteLine(link.Name + " " + link.SourceReferenceName + " " + link.TargetReferenceName + " " + link.SourceReferenceType + " " + link.TargetReferenceType);
                    Console.WriteLine("\t{0} - {1} => {2}", link.Name, link.Source, link.Target);
                }

                Console.WriteLine("\nPrinting formulas...");
                foreach (IFormula formula in group.Formulas)
                {
                    Console.WriteLine(formula.Name + " " + formula.Description + " " + formula.FormulaType);
                    foreach (IParameter par in formula.Parameters)
                    {
                        Console.WriteLine("\tParameter: " + par.Reference.Name + " " + par.Reference.ReferenceType);
                    }
                }
            }
        }

        static ITransformGroup ParsePage(XElement page)
        {
            string pageName = GetAttributeValue(page, "Name");
            ITransformGroup group = new TransformGroup(pageName);

            ParseListOfFunctoid(page, group);
            ParseListOfLink(page, group);

            return group;
        }

        static void ParseListOfFunctoid(XElement page, ITransformGroup group)
        {
            IEnumerable<XElement> functoids = page.Elements(XName.Get("Functoids"));
            IEnumerable<XElement> functoidList = null;
            foreach (XElement f in functoids)
            {
                functoidList = f.Elements(XName.Get("Functoid"));
                break;
            }

            foreach (XElement functoid in functoidList)
            {
                IFormula formula = ParseFunctoid(functoid);
                group.Formulas.Add(formula);
            }
        }

        static IFormula ParseFunctoid(XElement functoid)
        {
            string name = GetAttributeValue(functoid, "FunctoidID");
            string description = GetAttributeValue(functoid, "Functoid-FID");
            string moreDesc = GetAttributeValue(functoid, "Functoid-Name");
            FormulaType ftype = GetFormulaType(description);
            if (!string.IsNullOrEmpty(moreDesc))
            {
                description = string.Format("{0}:{1}", description, moreDesc);
            }

            IFormula formula = new Formula(name, description, ftype);

            XElement inputParameter = GetFirstElement(functoid, "Input-Parameters");
            IEnumerable<XElement> inputParamList = inputParameter.Elements("Parameter");
            foreach (XElement parm in inputParamList)
            {
                IParameter parameter = new Parameter();
                parameter.Reference.Name = GetAttributeValue(parm, "Value");
                parameter.Reference.ReferenceType = GetReferenceType(GetAttributeValue(parm, "Type"));
                parameter.LinkIndex = GetAttributeValue(parm, "linkIndex");
                formula.Parameters.Add(parameter);
            }

            return formula;
        }

        static ReferenceType GetReferenceType(string val)
        {
            ReferenceType referenceType;
            val = val.ToLower();
            switch (val)
            {
                case "link":
                referenceType = ReferenceType.Document;
                break;

                case "constant":
                referenceType = ReferenceType.Literal;
                break;

                default:
                throw new NotSupportedException("Invalid Type attribute found in Parameter");
            }

            return referenceType;
        }

        static FormulaType GetFormulaType(string desc)
        {
            FormulaType ftype = FormulaType.Equality;
            switch (desc)
            {
                case "102":
                    ftype = FormulaType.StringLeft;
                    break;

                case "104":
                    ftype = FormulaType.StringRight;
                    break;

                case "107":
                    ftype = FormulaType.Concatenate;
                    break;

                case "110":
                    ftype = FormulaType.Uppercase;
                    break;

                case "123":
                    ftype = FormulaType.Date;
                    break;

                case "124":
                    ftype = FormulaType.Time;
                    break;

                case "260":
                    ftype = FormulaType.Scripting;
                    break;
                
                //case "424": //loop, 
                //case "701": //conditional existence, both can be handled by producing/supressing nodes in output doc.
                //    ftype = FormulaType.Copy;
                //    break;

                case "424":
                    ftype = FormulaType.Looping;
                    break;

                case "701":
                    ftype = FormulaType.LogicalExistence;
                    break;

                case "315":
                    ftype = FormulaType.Equality;
                    break;

                case "320":
                    ftype = FormulaType.LogicalOr;
                    break;

                case "375":
                    ftype = FormulaType.ValueMapping;
                    break;

                case "311":
                    ftype = FormulaType.GreaterThan;
                    break;

                case "105":
                    ftype = FormulaType.Size;
                    break;

                case "322":
                    ftype = FormulaType.RecordCount;
                    break;

                case "118":
                    ftype = FormulaType.Addition;
                    break;

                case "108":
                case "109":
                    ftype = FormulaType.Trim;
                    break;

                case "317":
                    ftype = FormulaType.LogicalString;
                    break;

                case "316":
                    ftype = FormulaType.NotEqual;
                    break;

                case "321":
                    ftype = FormulaType.LogicalAnd;
                    break;

                case "703":
                    ftype = FormulaType.TableLooping;
                    break;

                case "704":
                    ftype = FormulaType.TableExtractor;
                    break;

                case "324":
                    ftype = FormulaType.CumulativeSum;
                    break;

                case "474":
                    ftype = FormulaType.Iteration;
                    break;

                case "802":
                    ftype = FormulaType.MassCopy;
                    break;

                case "319":
                    ftype = FormulaType.LogicalNumeric;
                    break;

                case "125":
                    ftype = FormulaType.DateAndTime;
                    break;

                case "323":
                    ftype = FormulaType.Index;
                    break;

                default:
                    ftype = FormulaType.NotSupported;
                    break;
                    //throw new NotSupportedException("Invalid Functoid found " + desc);
            }

            return ftype;
        }

        static void ParseListOfLink(XElement page, ITransformGroup group)
        {
            IEnumerable<XElement> links = page.Elements(XName.Get("Links"));
            IEnumerable<XElement> linkList = null;
            foreach (XElement l in links)
            {
                linkList = l.Elements(XName.Get("Link"));
                break;
            }

            foreach (XElement link in linkList)
            {
                ITransformLink docLink = ParseLink(link);
                group.Links.Add(docLink);
            }
        }

        static ITransformLink ParseLink(XElement link)
        {
            ReferenceType sourceReferenceType, targetReferenceType;
            string sourceReferenceName = RetrieveAndCondenseLink(link, "LinkFrom", out sourceReferenceType);
            string targetReferenceName = RetrieveAndCondenseLink(link, "LinkTo", out targetReferenceType);

            ITransformLink transformLink = new TransformLink(GetAttributeValue(link, "LinkID"));
            //Console.WriteLine(sourceReferenceName + " -> " + targetReferenceName);

            transformLink.Source.Name = sourceReferenceName;
            transformLink.Source.ReferenceType = sourceReferenceType;
            transformLink.Target.Name = targetReferenceName;
            transformLink.Target.ReferenceType = targetReferenceType;

            return transformLink;
        }

        static string RetrieveAndCondenseLink(XElement link, string attrName, out ReferenceType referenceType)
        {
            XAttribute attr = link.Attribute(XName.Get(attrName));
            string path = attr != null ? attr.Value : string.Empty;
            path = CondensePath(path, out referenceType);
            return path;
        }

        const string LocalName = "[local-name()=";
        const string Schema = "<Schema>";
        static string CondensePath(string path, out ReferenceType referenceType)
        {
            StringBuilder pathBuilder = new StringBuilder(50);
            string[] pathParts = path.Split('/', '*');

            if (pathParts.Length == 1)
            {
                //Functoid reference
                pathBuilder.Append(path);
                referenceType = ReferenceType.Formula;
            }

            else
            {
                referenceType = ReferenceType.Document;
                foreach (string pathPart in pathParts)
                {
                    if (pathPart.StartsWith(LocalName))
                    {
                        string nameWithQuote = pathPart.Substring(LocalName.Length + 1);
                        if (nameWithQuote.IndexOf(Schema) < 0)
                        {
                            if(pathBuilder.Length != 0)
                                pathBuilder.Append("->");
                            nameWithQuote = nameWithQuote.Substring(0, nameWithQuote.Length - 2);
                            pathBuilder.Append(nameWithQuote);
                        }

                    }
                }
            }

            return pathBuilder.ToString();
        }

        static void MarkForIgnore(ITransformGroup group)
        {
            Dictionary<string, ITransformLink> linkMap = new Dictionary<string, ITransformLink>();
            
            foreach (ITransformLink link in group.Links)
            {
                if (link.Source.ReferenceType == ReferenceType.Formula
                    || link.Target.ReferenceType == ReferenceType.Formula)
                {
                    linkMap[link.Name] = link;
                }
            }

            Dictionary<string, IFormula> ignoredFormulaMap = new Dictionary<string, IFormula>();
            ITransformLink referencedLink;
            foreach (IFormula formula in group.Formulas)
            {
                if (formula.FormulaType == FormulaType.Copy)
                {
                    formula.Ignore = true;
                    ignoredFormulaMap[formula.Name] = formula;
                    foreach (IParameter param in formula.Parameters)
                    {
                        if (param.Reference.ReferenceType == ReferenceType.Document)
                        {
                            linkMap.TryGetValue(param.Reference.Name, out referencedLink);
                            if (referencedLink != null)
                            {
                                referencedLink.Ignore = true;
                            }
                        }
                    }
                }
            }

            IFormula referencedFormula;
            //2nd pass over links to see which ones refer to ignored formula
            foreach (ITransformLink link in group.Links)
            {
                if (link.Ignore) continue;

                if (link.Source.ReferenceType == ReferenceType.Formula)
                {
                    ignoredFormulaMap.TryGetValue(link.Source.Name, out referencedFormula);
                    if (referencedFormula != null && referencedFormula.Ignore)
                    {
                        link.Ignore = true;
                    }
                }

                if (!link.Ignore && link.Target.ReferenceType == ReferenceType.Formula)
                {
                    ignoredFormulaMap.TryGetValue(link.Target.Name, out referencedFormula);
                    if (referencedFormula != null && referencedFormula.Ignore)
                    {
                        link.Ignore = true;
                    }
                }
            }
        }

        static string GetAttributeValue(XElement element, string attrName)
        {
            string val;
            XAttribute attr = element.Attribute(XName.Get(attrName));
            val = attr != null ? attr.Value : null;
            return val;
        }

        static XElement GetFirstElement(XElement element, string name)
        {
            XElement first = null;
            IEnumerable<XElement> list = element.Elements(XName.Get(name));

            foreach (XElement e in list)
            {
                first = e;
                break;
            }

            return first;
        }
    }
}
