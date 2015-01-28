using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maarg.Fatpipe.Plug.DataModel;
using System.IO;
using OfficeOpenXml;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class ExcelToDocumentPlug
    {
        const int NodeTypeIndex = 1;
        const int MandatoryIndex = 2;
        const int Level1NameIndex = 3;
        const int Level2NameIndex = 4;
        const int Level3NameIndex = 5;
        const int Level4NameIndex = 6;
        const int Level5NameIndex = 7;
        const int Level6NameIndex = 8;
        const int Level7NameIndex = 9;
        const int Level8NameIndex = 10;
        const int DescriptionIndex = 11;
        const int PathIndex = 12;
        const int RepeatableIndex = 13;

        Pluglet rootPluglet;
        ExcelWorksheet current;

       // static string RepositoryRoot  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\");

       
        public IDocumentPlug generateDocumentPlug(BusinessDomain domain)
        {
            IDocumentPlug plug = new DocumentPlug(rootPluglet, domain);
            return plug;
        }

        public string retreiveRootName(string type)
        {
          return (current.Name.Substring(type.Length, current.Name.Length - type.Length));
        }

        public void createRootPluglet(Stream stream, string type)
        {
            
            //path = Path.Combine(RepositoryRoot, @"sources\test\PlugTestHarness\output\Copy of Inbound850ToSuperspecPlug.xlsx");
            //fileInfo = new FileInfo(path);
            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorkbook workBook = pck.Workbook;
                
                //change worksheet index here for source or target
                if(type.Equals("Source#"))
                    current = workBook.Worksheets[1];
                else
                    current = workBook.Worksheets[2];


                rootPluglet = new Pluglet(retreiveRootName(type), "root Node", (PlugletType)Enum.Parse(typeof(PlugletType), "Segment"), null);

                int rowCount = current.Dimension.End.Row - current.Dimension.Start.Row + 2;

                for (int row = 2; row < rowCount; row++)
                {
                    if (current.Cells[row, NodeTypeIndex].Value == null)
                        continue;

                    string name = extractName(row);

                    createPluglet(name, current.Cells[row, DescriptionIndex].Value.ToString(), (PlugletType)Enum.Parse(typeof(PlugletType), current.Cells[row, NodeTypeIndex].Value.ToString()), retreiveParent(current.Cells[row, PathIndex].Value.ToString()), current.Cells[row, MandatoryIndex].Value.ToString(), current.Cells[row, RepeatableIndex].Value.ToString());

                }
            }

        }

        private void createPluglet(string name, string description, PlugletType type, IPluglet parent, string mandatory, string repeatable)
        {
            int maxOccurs = 1;
            int minOccurs = 1;
            if (repeatable.Equals("Y"))
                maxOccurs = -1;
            if (mandatory == "N")
                minOccurs = 0;
            //if (type == PlugletType.Loop)
              //  maxOccurs = -1;

            Pluglet pluglet = new Pluglet(name, description, type, parent, minOccurs, maxOccurs);

        }

        private string extractName(int row)
        {
            int counter = Level1NameIndex;

            while (true)
            {
                if (current.Cells[row, counter].Value!=null)
                    return current.Cells[row, counter].Value.ToString();
                counter++;
            }
        }

        

        private IPluglet retreiveParent(string parentPath)
        {
            parentPath = parentPath.Replace("->", "/");

            return searchForNode(rootPluglet.Children, parentPath.Split('/'), 1, rootPluglet);

        }

        private IPluglet searchForNode(IList<IPluglet> Children, string[] nodes, int index, IPluglet result)
        {

            if (index == nodes.Length - 1)
                return result;
            foreach (IPluglet child in Children)
            {
                if (child.Name.Equals(nodes[index]))
                {
                    return searchForNode(child.Children, nodes, index + 1, child);
                }
            }
            return null;
        }

    }
}
