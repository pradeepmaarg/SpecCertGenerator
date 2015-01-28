using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Maarg.Fatpipe.LoggingService;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.FlatFilePlug
{
    public class FlatFileReader : IFlatFileReader
    {
        private static readonly ILogger Logger = LoggerFactory.Logger;
        private InterchangeErrors errors;

        public InterchangeErrors Errors { get {return errors; } }
        
        private Delimiters FlatFileDelimiters { get; set; }
        private FatpipeDocument FatpipeDocumentInst { get; set; }
        private StreamReader DocumentReader { get; set; }
        private IDocumentPlug DocumentPlug { get; set; }
        private IPluglet CurrentPluglet { get; set; }
        private string[] CurrentSegmentDetails { get; set; }

        // properties used in traversing document
        private string CurrentSegment { get; set; }
        private int SegmentNumberFromStart { get; set; }
        private long CurrentSegmentStartPos { get; set; }
        private long CurrentSegmentEndPos { get; set; }
        private int FunctionalGroupNumber { get; set; }
        private int TransactionSetNumber { get; set; }
        private int ValidTransactionSetCount { get; set; }
        private int InvalidTransactionSetCount { get; set; }

        // Properties to store original payload
        private string TransactionSegment { get; set; }
        private string SegmentDelimiter { get; set; }
        private string FormattedTransactionSegment { get; set; }
        private string FormattedSegmentDelimiter { get; set; }
        private string BeautifiedOriginalPayload;

        /// <summary>
        /// Return flat file input and construct IFatpipeDocument
        /// IFatpipeDocument will NOT contain ISA and GA segments as it's not 
        /// present in case of flat file.
        /// Since ISA segment is missing and there is no speific ST segment
        /// DocumentPlug is mandatory parameter.
        /// </summary>
        /// <returns></returns>
        public IFatpipeDocument ReadFile(Stream flatFileStream, IDocumentPlug documentPlug)
        {
            if (flatFileStream == null)
                throw new ArgumentNullException("flatFileStream", "Flat file stream cannot be null");

            if (documentPlug == null)
                throw new ArgumentNullException("documentPlug", "Document plug cannot be null");

            string location = "FlatFileReader.ReadFile";
            Logger.Debug(location, "Start");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            errors = new InterchangeErrors();
            DocumentPlug = documentPlug;

            long orgStartPos = flatFileStream.Position;

            FlatFileDelimiters = InitializeDelimiters(documentPlug);
            SegmentDelimiter = ((char)FlatFileDelimiters.SegmentDelimiter).ToString();
            FormattedSegmentDelimiter = SegmentDelimiter;

            bool crLFPresent = FlatFileDelimiters.SegmentDelimiter == Delimiters.CarriageReturn || FlatFileDelimiters.SegmentDelimiterSuffix1 == Delimiters.CarriageReturn;
            if (!crLFPresent)
            {
                FormattedSegmentDelimiter = string.Format("{0}{1}", FormattedSegmentDelimiter, Environment.NewLine);
            }

            SegmentNumberFromStart = 0;
            CurrentSegmentStartPos = 0;

            this.DocumentReader = new StreamReader(flatFileStream, Encoding.UTF8);

            // Since flat file doesn't have concept of ST/SE, ans we want to use InterchangeErrors for reporting purpose
            // create dummy transaction set details
            Errors.AddTransactionSetDetails(1, "", "", true);

            CurrentPluglet = DocumentPlug.RootPluglet;
            CurrentPluglet.ResetCurrentOccurances(); // do we need this for flat files?
            CurrentPluglet.InitializeStartSegmentList();

            FatpipeDocumentInst = new FatpipeDocument();
            //FatpipeDocumentInst.TransactionSetType = ???;
            FatpipeDocumentInst.DocumentPlug = DocumentPlug;
            FatpipeDocumentInst.RootFragment = CurrentPluglet.ConstructDocumentFragment(null, null);

            while ((CurrentSegmentDetails = ReadNextSegment()) != null
                    && CurrentSegmentDetails.Length > 0)
            {
                Logger.Debug(location, "{0} - Next segment {1}", GetCurrentPosContext(), CurrentSegmentDetails[0]);

                CreateAndAddNewSegment(CurrentSegmentDetails[0], CurrentSegmentDetails);

                if(string.IsNullOrWhiteSpace(FatpipeDocumentInst.OriginalPayload))
                    FatpipeDocumentInst.OriginalPayload = CurrentSegment;
                else
                    FatpipeDocumentInst.OriginalPayload += SegmentDelimiter + CurrentSegment;

                if (string.IsNullOrWhiteSpace(FatpipeDocumentInst.BeautifiedOriginalPayloadBody))
                    FatpipeDocumentInst.BeautifiedOriginalPayloadBody = CurrentSegment;
                else
                    FatpipeDocumentInst.BeautifiedOriginalPayloadBody += FormattedSegmentDelimiter + CurrentSegment;
            }

            sw.Stop();
            Logger.Debug(location, "Stop - Elapsed time {0} ms", sw.ElapsedMilliseconds);

            return FatpipeDocumentInst;
        }

        private static Delimiters InitializeDelimiters(IDocumentPlug documentPlug)
        {
            int componentSeparator = 0; // Flat file does not have have components
            int segmentDelimiterSuffix1 = 0;
            int segmentDelimiterSuffix2 = 0;

            if (documentPlug.SegmentDelimiters.Count > 1)
                segmentDelimiterSuffix1 = documentPlug.SegmentDelimiters[1];

            if (documentPlug.SegmentDelimiters.Count > 2)
                segmentDelimiterSuffix2 = documentPlug.SegmentDelimiters[2];

            return new Delimiters(documentPlug.ElementDelimiters[0], componentSeparator,
                documentPlug.SegmentDelimiters[0], segmentDelimiterSuffix1, segmentDelimiterSuffix2);
        }

        /// <summary>
        /// Find corrosponding Pluglet for given segmentname and create new segment
        /// </summary>
        /// <param name="segmentName"></param>
        /// <param name="segmentDetails"></param>
        private void CreateAndAddNewSegment(string segmentName, string[] segmentDetails)
        {
            string firstSegmentName = string.Empty;
            string errorMsgs = string.Empty;
            IPluglet nextPluglet;

            string location = "FlatFileReader.CreateAndAddNewSegment";
            Logger.Debug(location, "Adding {0} segment", segmentName);

            string missingMandatorySegments;
            nextPluglet = CurrentPluglet.GetSegmentPluglet(segmentName, segmentDetails, firstSegmentName, out missingMandatorySegments);

            // First add missing mandatory segment errors
            if (nextPluglet != null && !string.IsNullOrWhiteSpace(missingMandatorySegments))
            {
                string error = string.Format("Missing mandatory segments ({0}) between {1} and {2}"
                                    , missingMandatorySegments, CurrentPluglet.Tag, segmentName);

                if (FatpipeDocumentInst.Errors == null)
                    FatpipeDocumentInst.Errors = new List<string>();
                FatpipeDocumentInst.Errors.Add(error);

                Logger.Error("FlatFileReader.CreateAndAddNewSegment", EventId.EDIReaderMissingMandatorySegment, "{0} - {1}", GetCurrentPosContext(), error);

                EdiErrorType errorType = nextPluglet.IsIgnore ? EdiErrorType.Warning : EdiErrorType.Error;

                foreach (string segment in missingMandatorySegments.Split(','))
                {
                    Errors.AddSegmentError(segmentName, X12ErrorCode.MandatorySegmentMissingCode
                        , string.Format("{0} : {1}", X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.MandatorySegmentMissingCode), segment)
                        , SegmentNumberFromStart, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, errorType);
                }
            }

            if (nextPluglet == null)
            {
                /* //TODO: Revisit following if condition - do we really want to ignore ISA/GS segment missing in schema?
                 if (segmentName != "ISA" && segmentName != "GS")
                 {
                     errors = string.Format("{0} segment not found in schema after {1}", segmentName, CurrentPluglet.Name);

                     Logger.Error(location, EventId.EDIReaderUnknownSegment, "{0} - {1}", GetCurrentPosContext(), errors);
                     Errors.AddSegmentError(segmentName, X12ErrorCode.UnrecognizedSegmentIDCode
                         , X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnrecognizedSegmentIDCode), SegmentNumber);
                 }
                 */
                //experimenting with unknown here above is actual
                //IPluglet unknown = new Pluglet("UNRECOGNIZED_SEGMENT", "Unknown Segment", PlugletType.Segment, CurrentPluglet.Parent);
                IPluglet unknown = new Pluglet(
                        new PlugletInput() 
                        {
                            Name = "UNRECOGNIZED_SEGMENT",
                            Definition = "Unknown Segment",
                            Type = PlugletType.Segment,
                            Parent = CurrentPluglet.Parent,
                            IsIgnore = false,
                            AddToParent = false,
                            IsTagSameAsName = true,
                        } );

                //for(int i = 1; i < segmentDetails.Length; i++)
                //{
                //  IPluglet x = new Pluglet("child"+i, "Unknown Data", PlugletType.Data, unknown);
                //unknown.Children.Add(x);
                //}

                //DocumentFragment newFragment = unknown.ConstructDocumentFragment(segmentDetails, false, FlatFileDelimiters, out errors);

                errorMsgs = string.Format("{0} segment not found in schema after {1}", segmentName, CurrentPluglet.Name);

                Errors.AddSegmentError(segmentName, X12ErrorCode.UnrecognizedSegmentIDCode
                    , X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnrecognizedSegmentIDCode), SegmentNumberFromStart
                    , this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, EdiErrorType.Error);

                DocumentFragment newFragment = new DocumentFragment()
                {
                    Pluglet = unknown,
                    Children = new List<IDocumentFragment>(),
                };

                IPluglet childPluglet = new Pluglet("Data", "Data", PlugletType.Data, null);
                DocumentFragment child = new DocumentFragment()
                {
                    Parent = newFragment,
                    Pluglet = childPluglet,
                    Children = null,
                };

                newFragment.Children.Add(child);
                child.Value = CurrentSegment;

                if (newFragment == null)
                {
                    errorMsgs = string.Format("{0} DocumentFragment creation failed. Errors: {1}", segmentName, errorMsgs);
                    Logger.Error(location, EventId.EDIReaderDocFragmentCreation, "{0} - {1}", GetCurrentPosContext(), errorMsgs);
                    //TODO: what should be the code here?
                    //Errors.AddGenericError(segmentName, X12ErrorCode.???
                }
                else
                {
                    ((DocumentFragment)FatpipeDocumentInst.RootFragment).AddDocumentFragment(newFragment);

                    // CurrentPluglet = newFragment.Pluglet;
                }

                //experimenting with unknown here
            }
            else
            {
                if (nextPluglet.RepetitionInfo.MaxOccurs == 0)
                {
                    Errors.AddSegmentError(segmentName, X12ErrorCode.UnexpectedSegmentCode
                        , string.Format("{0} : {1}", X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnexpectedSegmentCode), nextPluglet.Tag)
                        , SegmentNumberFromStart, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, EdiErrorType.Error);
                }
                else
                {
                    DocumentFragment newFragment = nextPluglet.ConstructDocumentFragment(segmentDetails, false, FlatFileDelimiters,
                        SegmentNumberFromStart, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, ref errors, out errorMsgs);

                    if (newFragment == null)
                    {
                        //errorMsgs = string.Format("{0} DocumentFragment creation failed. Errors: {1}", segmentName, errorMsgs);
                        Logger.Error(location, EventId.EDIReaderDocFragmentCreation, "{0} - {1}", GetCurrentPosContext(), errorMsgs);
                        // TODO: Replace UnexpectedSegmentCode with appropriate one
                        Errors.AddGenericError(segmentName, X12ErrorCode.UnexpectedSegmentCode, errorMsgs,
                            SegmentNumberFromStart, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos);
                    }
                    else
                    {
                        ((DocumentFragment)FatpipeDocumentInst.RootFragment).AddDocumentFragment(newFragment);

                        CurrentPluglet = newFragment.Pluglet;
                    }
                }
            }

            if (!string.IsNullOrEmpty(errorMsgs))
            {
                if (FatpipeDocumentInst.Errors == null)
                    FatpipeDocumentInst.Errors = new List<string>();
                FatpipeDocumentInst.Errors.Add(errorMsgs);
            }
        }

        private string[] ReadNextSegment()
        {
            string segment = DocumentReader.ReadLine((char)FlatFileDelimiters.SegmentDelimiter, true);

            if (string.IsNullOrEmpty(segment))
                return null;

            CurrentSegment = segment;

            if(this.CurrentSegmentEndPos != 0)
                this.CurrentSegmentStartPos = this.CurrentSegmentEndPos + 1;

            this.CurrentSegmentEndPos = this.CurrentSegmentStartPos + 
                (segment.Length + FlatFileDelimiters.SegmentDelimiterLength - 1); // segment delimiter has variable length due to optional CR LF

            string format = "Segment: {0}, StartPos: {1}, EndPos {2}";
            format = string.Format(format, SegmentNumberFromStart, CurrentSegmentStartPos, CurrentSegmentEndPos);
            //Console.WriteLine(format);
            SegmentNumberFromStart++;

            return segment.Split((char)FlatFileDelimiters.FieldSeperator);
        }

        private string GetCurrentPosContext()
        {
            return string.Format("Segment#:{0}", SegmentNumberFromStart);
        }
    }
}