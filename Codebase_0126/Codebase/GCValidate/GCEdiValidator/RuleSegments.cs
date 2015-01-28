using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    class RuleSegments
    {
        public string FileName { get; set; }
        public IFatpipeDocument FatpipeDocument { get; set; }
        public Dictionary<string, List<IDocumentFragment>> UsedSegments { get; private set; }
        public Dictionary<string, IDocumentFragment> CurrentSegments { get; private set; }
        public Dictionary<string, List<IDocumentFragment>> UnusedSegments { get; private set; }

        public string CertName { get { return Path.GetFileNameWithoutExtension(FileName); } }

        public RuleSegments(IFatpipeDocument fatpipeDocument, string fileName)
        {
            FileName = fileName;
            FatpipeDocument = fatpipeDocument;
            UsedSegments = new Dictionary<string, List<IDocumentFragment>>();
            CurrentSegments = new Dictionary<string, IDocumentFragment>();
            UnusedSegments = new Dictionary<string, List<IDocumentFragment>>();
        }

        public IDocumentFragment SelectSegment(SegmentPath segmentPath, string value)
        {
            IDocumentFragment segmentDocumentFragment = null;

            // If SegmentPath is conditional group then replace value with conditional value
            if (string.IsNullOrWhiteSpace(segmentPath.Value) == false)
                value = segmentPath.Value;

            if (CurrentSegments.TryGetValue(segmentPath.Path, out segmentDocumentFragment) == false)
            {
                List<IDocumentFragment> unusedSegmentsList;
                if (UnusedSegments.TryGetValue(segmentPath.Path, out unusedSegmentsList) == false)
                {
                    PopulateUnusedSegments(segmentPath);
                }

                List<IDocumentFragment> documentFragments;
                if (UnusedSegments.TryGetValue(segmentPath.Path, out documentFragments))
                {
                    if(documentFragments.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(value) == false && documentFragments.Count != 1)
                        {
                            foreach (IDocumentFragment documentFragment in documentFragments)
                            {
                                string segmentValue = documentFragment.GetDataSegmentValue(segmentPath.DataSegmentName);
                                if (string.Equals(segmentValue, value, StringComparison.OrdinalIgnoreCase))
                                {
                                    segmentDocumentFragment = documentFragment;
                                }
                            }
                        }
                        else
                        {
                            segmentDocumentFragment = documentFragments[0];
                        }

                        if (segmentDocumentFragment != null)
                        {
                            documentFragments.Remove(segmentDocumentFragment);
                            CurrentSegments.Add(segmentPath.Path, segmentDocumentFragment);
                        }
                    }
                }
            }

            return segmentDocumentFragment;
        }

        public void MoveCurrentToUsed()
        {
            List<IDocumentFragment> documentFragments;
            foreach (string path in CurrentSegments.Keys)
            {
                if (UsedSegments.TryGetValue(path, out documentFragments) == false)
                {
                    documentFragments = new List<IDocumentFragment>();
                    UsedSegments.Add(path, documentFragments);
                }
                documentFragments.Add(CurrentSegments[path]);
            }

            CurrentSegments.Clear();
        }

        // Assumptions:
        // 1. Loop DocumentFragment name ends with "Loop" e.g. "N1Loop"
        private void PopulateUnusedSegments(SegmentPath segmentPath)
        {
            List<IDocumentFragment> intermediateDocumentFragments = new List<IDocumentFragment>();
            List<IDocumentFragment> documentFragments = new List<IDocumentFragment>();

            if (FatpipeDocument.RootFragment != null && FatpipeDocument.RootFragment.Children != null)
            {
                documentFragments.Add(FatpipeDocument.RootFragment);

                // Loop document fragments are special, add all loop fragments to list
                foreach (IDocumentFragment loop in FatpipeDocument.RootFragment.Children)
                {
                    if (string.Equals(loop.Pluglet.Tag, "loop", StringComparison.OrdinalIgnoreCase))
                        documentFragments.Add(loop);
                }

                string segmentName;
                for (int i = 0; i < segmentPath.Segments.Count; i++)
                {
                    segmentName = segmentPath.Segments[i];
                    if (i < segmentPath.Segments.Count - 1)
                        segmentName = segmentName + "Loop";

                    foreach (IDocumentFragment documentFragment in documentFragments)
                    {
                        foreach (IDocumentFragment child in documentFragment.Children)
                        {
                            if (string.Equals(child.Pluglet.Tag, segmentName, StringComparison.OrdinalIgnoreCase))
                            {
                                intermediateDocumentFragments.Add(child);
                            }
                        }
                    }

                    /*loopDocumentFragments.Clear();
                    loopDocumentFragments.AddRange(intermediateDocumentFragments);
                    intermediateDocumentFragments.Clear();*/
                    documentFragments = intermediateDocumentFragments;
                    intermediateDocumentFragments = new List<IDocumentFragment>();
                }
            }

            UnusedSegments.Add(segmentPath.Path, documentFragments);
        }
    }
}
