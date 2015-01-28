using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Fatpipe.LoggingService;
using Maarg.Fatpipe.Plug.DataModel;
using System.Diagnostics;

namespace Maarg.Fatpipe.DocumentTransformer
{
    public class DocumentTransformer
    {
        private static readonly ILogger Logger = LoggerFactory.Logger;

        public static IFatpipeDocument Transform(IFatpipeDocument sourceDocument, ITransformPlug transformPlug)
        {
            if (sourceDocument == null)
                throw new ArgumentNullException("sourceDocument");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            FatpipeDocument targetDocument = new FatpipeDocument();
            targetDocument.RootFragment = transformPlug.TargetDocument.RootPluglet.ConstructDocumentFragment(null, null);;

            //TODO: Handle loops
            foreach (ITransformGroup transformGroup in transformPlug.Facets)
            {
                Logger.Debug("DocumentTransformer.Transform", "Processing {0} tranformGroup", transformGroup.Name);

                foreach (ITransformLink transformLink in transformGroup.Links)
                {
                    Logger.Debug("DocumentTransformer.Transform", "Processing Link#-{0}: {1} [{2}] => {3} [{4}]"
                        , transformLink.Name
                        , transformLink.Source.ReferenceType, transformLink.Source.Name
                        , transformLink.Target.ReferenceType, transformLink.Target.Name);

                    //TODO: Handle all kind of transformation
                    if (transformLink.Source.ReferenceType == ReferenceType.Document
                        && transformLink.Target.ReferenceType == ReferenceType.Document)
                    {
                        // Traverse source path
                        IDocumentFragment sourceFragment = sourceDocument.RootFragment.MoveTo(transformLink.Source.Name);
                        if (sourceFragment != null)
                        {
                            IPluglet targetPluglet = targetDocument.RootFragment.Pluglet.MoveTo(transformLink.Target.Name);

                            if (targetPluglet != null)
                            {
                                Logger.Debug("DocumentTransformer.Transform", "Source = {0}, Target = {1}", sourceFragment.Name, targetPluglet.Tag);

                                Dictionary<string, string> attributes = new Dictionary<string, string>();
                                foreach (IPluglet attr in targetPluglet.Attributes)
                                {
                                    string attributeName = attr.Name;
                                    attributeName.Remove(0, 1);
                                    attributeName.Remove(attributeName.Length, 1);
                                    attributes.Add(attributeName, attributeName);
                                }

                                bool isAttribute = false;
                                string name = transformLink.Target.Name.Substring(
                                                    transformLink.Target.Name.LastIndexOf(targetPluglet.PathSeperator) + targetPluglet.PathSeperator.Length);
                                // Check if this transformation point to attribute or leaf node
                                if(targetPluglet.Attributes != null && attributes.ContainsKey(name))
                                    isAttribute = true;

                                if (isAttribute == false)
                                {
                                    IDocumentFragment newFragment = targetPluglet.ConstructDocumentFragment(null, sourceFragment.Value);
                                    ((DocumentFragment)targetDocument.RootFragment).AddDocumentFragment(newFragment);
                                }
                                else
                                {
                                    ((DocumentFragment)targetDocument.RootFragment).AddDocumentFragment(transformLink.Target.Name, sourceFragment.Value);
                                }
                            }
                            else
                            {
                                string error = string.Format("Link#-{0}: {1} path not found in target tree", transformLink.Name, transformLink.Target.Name);
                                if (targetDocument.Errors == null)
                                    targetDocument.Errors = new List<string>();
                                targetDocument.Errors.Add(error);
                                
                                Logger.Error("DocumentTransformer.Transform", EventId.DocTransformerNoMapping, error);
                            }
                        }
                    }
                    else
                    {
                        Logger.Debug("DocumentTransformer.Transform", "Ignoring Link#-{0}", transformLink.Name);
                    }
                }

                //TODO: Handle transformGroup.Formulas
            }

            sw.Stop();
            Logger.Debug("DocumentTransformer.Transform", "Stop. Elapsed time {0} ms", sw.ElapsedMilliseconds);

            return targetDocument;
        }
    }
}
