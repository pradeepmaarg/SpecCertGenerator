using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.EDIPlug
{
    enum EDIState
    {
        None,
        ISA,
        GS,
        ST,
        SE,
        GE,
        IEA,
        Other
    }

    public class EDIReader : IEDIReader
    {
        // Schema cache
        private static Dictionary<string, IDocumentPlug> SchemaCache = new Dictionary<string, IDocumentPlug>();

        // 17 since we store ISA as first field
        const int MaxISAFieldRecordCount = 17;
        const int SourceFieldNumber = 10;
        const int TargetFieldNumber = 12;

        InterchangeErrors errors;

        private static readonly ILogger Logger = LoggerFactory.Logger;

        #region Properties
        private IFatpipeManager FPManager { get; set; }

        public Delimiters EDIDelimiters { get; set; }

        public int TransactionSetNumber { get; set; }
        public int ValidTransactionSetCount { get; set; }
        public int InvalidTransactionSetCount { get; set; }

        private FatpipeDocument FatpipeDocumentInst { get; set; }

        public InterchangeErrors Errors 
        {
            get { return errors; }
            private set { errors = value; }
        }

        private StreamReader DocumentReader { get; set; }
        public IDocumentPlug DocumentPlug { get; set; }
        private IPluglet CurrentPluglet { get; set; }
        private string[] ISARecordFields { get; set; }
        private string[] GSRecordFields { get; set; }
        private string[] CurrentSegmentDetails { get; set; }

        // properties used in traversing document
        private EDIState LastState { get; set; }
        private bool GSSegmentProcessed { get; set; }
        private IPluglet GSPluglet;
        private int PrevTransactionSetType { get; set; }
        private string CurrentSegment { get; set; }
        private int SegmentNumberFromStart { get; set; }
        private int SegmentNumber { get; set; }
        private long CurrentSegmentStartPos { get; set; }
        private long CurrentSegmentEndPos { get; set; }
        private int FunctionalGroupNumber { get; set; }

        // Properties to store original payload
        private string ISASegment { get; set; }
        private string GSSegment { get; set; }
        private string TransactionSegment { get; set; }
        private string SegmentDelimiter { get; set; }
        private string FormattedTransactionSegment { get; set; }
        private string FormattedSegmentDelimiter { get; set; }
        #endregion

        #region IEDIReader Methods

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>true if EDI document otherwise false</returns>
        public bool Initialize(Stream stream)
        {
            return Initialize(stream, null, null);
        }

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fatpipeManager"></param>
        /// <returns>true if EDI document otherwise false</returns>
        public bool Initialize(Stream stream, IFatpipeManager fatpipeManager)
        {
            return Initialize(stream, fatpipeManager, null);
        }

        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fatpipeManager"></param>
        /// <param name="documentPlug"></param>
        /// <returns>true if EDI document otherwise false</returns>
        public bool Initialize(Stream stream, IFatpipeManager fatpipeManager, IDocumentPlug documentPlug)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Errors = new InterchangeErrors();

            FPManager = fatpipeManager;
            DocumentPlug = documentPlug;

            long orgStartPos = stream.Position;

            Delimiters delimiters;
            InterchangeTokenizer tokenizer = new InterchangeTokenizer(stream);
            bool isValidEDIDocument = tokenizer.IsX12Interchange(out delimiters);

            FunctionalGroupNumber = TransactionSetNumber = SegmentNumberFromStart = SegmentNumber = 0;
            ValidTransactionSetCount = InvalidTransactionSetCount = 0;
            PrevTransactionSetType = 0;

            DocumentReader = null;
            if (isValidEDIDocument == true)
            {
                EDIDelimiters = delimiters;
                //TODO: Review following logic
                //Read ISA field till component separator - Do not include component separator
                ISASegment = tokenizer.ISARecord.Substring(0, tokenizer.ISARecord.IndexOf((char)EDIDelimiters.ComponentSeperator) + 1);

                //TODO: Suraj: confirm this special case - last value as data element separator
                ISARecordFields = ISASegment.Split((char)EDIDelimiters.FieldSeperator);

                SegmentNumberFromStart = 1;

                stream.Position = orgStartPos;
                this.DocumentReader = new StreamReader(stream, Encoding.UTF8);
                //TODO: Why is seek required here?
                this.CurrentSegmentStartPos = 0;
                this.CurrentSegmentEndPos = 0;
                //this.CurrentSegmentEndPos = this.CurrentSegmentStartPos + tokenizer.ISARecordLen - 1;
                //this.DocumentReader.BaseStream.Seek(this.CurrentSegmentEndPos + basePosition, SeekOrigin.Begin);
                FatpipeDocumentInst = new FatpipeDocument();
                FatpipeDocumentInst.BeautifiedOriginalPayloadStartHeader = string.Empty;
                FatpipeDocumentInst.BeautifiedOriginalPayloadBody = string.Empty;
                FatpipeDocumentInst.BeautifiedOriginalPayloadEndHeader = string.Empty;

                SegmentDelimiter = ((char)EDIDelimiters.SegmentDelimiter).ToString();
                FormattedSegmentDelimiter = SegmentDelimiter;

                bool crLFPresent = delimiters.SegmentDelimiter == Delimiters.CarriageReturn || delimiters.SegmentDelimiterSuffix1 == Delimiters.CarriageReturn;
                if (!crLFPresent)
                {
                    FormattedSegmentDelimiter = string.Format("{0}{1}", FormattedSegmentDelimiter, Environment.NewLine);
                }

                Logger.Info("EDIReader.Initialize", "EDIDelimiters: SegmentDelimiter={0}, FieldSeperator={1}, ComponentSeperator={2}", (char)EDIDelimiters.SegmentDelimiter, (char)EDIDelimiters.FieldSeperator, (char)EDIDelimiters.ComponentSeperator);
            }
            else //invalid document code path
            {
                this.CurrentSegmentStartPos = orgStartPos;
                this.CurrentSegmentEndPos = orgStartPos + 3;
                Logger.Error("EDIReader.Initialize", EventId.EDIReaderInvalidDocument, "EDI document is not valid. Error: {0}", tokenizer.Error);

                Errors.AddGenericError("ISA", SchemaErrorCode.SchemaCode100EInvalidDocType, tokenizer.Error, SegmentNumber, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos);

                StringBuilder sb = new StringBuilder();
                int errorIndex = 1;
                Errors.IsaIeaErrorList.WriteError(sb, ref errorIndex);

                ISASegment = string.Empty;
            }

            LastState = EDIState.ISA;

            sw.Stop();
            Logger.Debug("EDIReader.Initialize", "Elapsed time {0} ms", sw.ElapsedMilliseconds);

            return isValidEDIDocument;
        }

        /// <summary>
        /// Return next EDI transaction set.
        /// IFatpipeDocument will contain ISA, GA and 1 ST segment
        /// </summary>
        /// <returns></returns>
        public IFatpipeDocument GetNextTransactionSet()
        {
            if (DocumentReader == null)
                throw new EDIReaderException("Initialize API should be invoked successfully before calling GetNextTransactionSet");

            string location = "EDIReader.GetNextTransactionSet";
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Logger.Debug(location, "Start - {0}", GetCurrentPosContext());

            bool isTransactionSetFinished = false;

            while (isTransactionSetFinished == false 
                    && (CurrentSegmentDetails = ReadNextSegment()) != null
                    && CurrentSegmentDetails.Length > 0)
            {
                Logger.Debug(location, "{0} - Next segment {1}", GetCurrentPosContext(), CurrentSegmentDetails[0]);

                EDIState nextState = GetEDIState(CurrentSegmentDetails[0]);

                switch (nextState)
                {
                    case EDIState.GS:
                        if (!(LastState == EDIState.ISA || LastState == EDIState.GE))
                            throw new EDIReaderException(string.Format("GS segment should appear only after ISA or GE. {1}", GetCurrentPosContext()));
 
                        //Just set GSRecordFields, GS node will be constructed on first ST node
                        GSRecordFields = CurrentSegmentDetails;
                        GSSegmentProcessed = false;

                        GSSegment = CurrentSegment;
                        FunctionalGroupNumber++;

                        // TODO: What is functionalId and ControlNumber? - set second and third parameter below
                        Errors.AddFunctionalGroupDetails(FunctionalGroupNumber, "", "", true);
                        break;

                    case EDIState.GE:
                        // TODO: Remove GS check here in case empty GS segment is not valid scenario
                        if (!(LastState == EDIState.GS || LastState == EDIState.SE))
                            throw new EDIReaderException(string.Format("GE segment should appear only after GS or SE. {1}", GetCurrentPosContext()));

                        //TODO: Add validation of GE segment details values
                        //CheckForMissingSegments(CurrentSegmentDetails[0]);
                        break;

                    case EDIState.ST:
                        if (!(LastState == EDIState.GS || LastState == EDIState.SE))
                            throw new EDIReaderException(string.Format("ST segment should appear only after GS or SE. {1}", GetCurrentPosContext()));

                        // Do not reset SegmentNumber as it's being used as over all segment number in EdiValidator for position
                        //SegmentNumber = 0;
                        TransactionSetNumber++;

                        // TODO: What is functionalId and ControlNumber? - set second and third parameter below
                        Errors.AddTransactionSetDetails(TransactionSetNumber, "", "", true);

                        FatpipeDocumentInst.BeautifiedOriginalPayloadBody = string.Empty;
                        TransactionSegment = CurrentSegment;
                        FormattedTransactionSegment = CurrentSegment;

                        // If ST segment is not valid then move file reading pointer to SE segment
                        if (ProcessSTSegment() == false)
                        {
                            MoveToSESegment();
                            isTransactionSetFinished = true;
                            nextState = EDIState.SE;
                        }

                        break;

                    case EDIState.SE:
                        if (!(LastState == EDIState.ST || LastState == EDIState.Other))
                            throw new EDIReaderException(string.Format("{0} segment should appear only after ST or Other. {1}", LastState, GetCurrentPosContext()));

                        //CheckForMissingSegments(CurrentSegmentDetails[0]);
                        CreateAndAddNewSegment(CurrentSegmentDetails[0], CurrentSegmentDetails);

                        //TODO: Add validation of SE segment details values

                        isTransactionSetFinished = true;

                        TransactionSegment = TransactionSegment + SegmentDelimiter + CurrentSegment;
                        FormattedTransactionSegment = FormattedTransactionSegment + FormattedSegmentDelimiter + CurrentSegment;

                        //ValidateContingencies();
                        break;

                    case EDIState.Other:
                        if (!(LastState == EDIState.ST || LastState == EDIState.Other))
                            throw new EDIReaderException(string.Format("{0} segment should appear only after ST or Other. {1}", LastState, GetCurrentPosContext()));

                        //Console.WriteLine("segment = " + CurrentSegmentDetails[0]);
                        CreateAndAddNewSegment(CurrentSegmentDetails[0], CurrentSegmentDetails);
                        
                        TransactionSegment = TransactionSegment + SegmentDelimiter + CurrentSegment;
                        FormattedTransactionSegment = FormattedTransactionSegment + FormattedSegmentDelimiter + CurrentSegment;
                        break;

                    case EDIState.IEA:

                        //TODO: Add validation of IEA segment details values
                        break;
                }

                LastState = nextState;
            }

            // Construct original payload
            if (isTransactionSetFinished == true)
            {
                FatpipeDocumentInst.OriginalPayload = ISASegment + SegmentDelimiter;
                FatpipeDocumentInst.OriginalPayload += GSSegment + SegmentDelimiter;
                FatpipeDocumentInst.OriginalPayload += TransactionSegment + SegmentDelimiter;
                FatpipeDocumentInst.OriginalPayload += "GE" + EDIDelimiters.FieldSeperator + "1" + EDIDelimiters.FieldSeperator + "1" + SegmentDelimiter;
                FatpipeDocumentInst.OriginalPayload += "IEA" + EDIDelimiters.FieldSeperator + "1" + EDIDelimiters.FieldSeperator + "1" + SegmentDelimiter;

                FatpipeDocumentInst.BeautifiedOriginalPayloadStartHeader = ISASegment + FormattedSegmentDelimiter;
                FatpipeDocumentInst.BeautifiedOriginalPayloadStartHeader += GSSegment + FormattedSegmentDelimiter;
                FatpipeDocumentInst.BeautifiedOriginalPayloadBody += FormattedTransactionSegment + FormattedSegmentDelimiter;
                FatpipeDocumentInst.BeautifiedOriginalPayloadEndHeader = "GE" + EDIDelimiters.FieldSeperator + "1" + EDIDelimiters.FieldSeperator + "1" + FormattedSegmentDelimiter;
                FatpipeDocumentInst.BeautifiedOriginalPayloadEndHeader += "IEA" + EDIDelimiters.FieldSeperator + "1" + EDIDelimiters.FieldSeperator + "1" + FormattedSegmentDelimiter;
            }

            sw.Stop();
            Logger.Debug(location, "Stop - {0}. Elapsed time {1} ms", GetCurrentPosContext(), sw.ElapsedMilliseconds);

            return isTransactionSetFinished == true ? FatpipeDocumentInst : null;
        }

        /// <summary>
        /// Return GS 3rd data segment value
        /// </summary>
        /// <returns></returns>
        public string GetSenderId()
        {
            if(GSRecordFields == null || GSRecordFields.Length < 4)
                return null;

            return GSRecordFields[3];
        }

        #endregion

        #region Private Methods

        // Return value indicate if ST segment is valid or not
        private bool ProcessSTSegment()
        {
            string location = "EDIReader.ProcessSTSegment";
            string errors = string.Empty;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            LastState = EDIState.ST;
            int currentTransactionSetType;

            if (CurrentSegmentDetails == null || CurrentSegmentDetails.Length < 2
                || int.TryParse(CurrentSegmentDetails[1], out currentTransactionSetType) == false)
            {
                InvalidTransactionSetCount++;

                //TODO: Add error
                Logger.Error(location, EventId.EDIReaderInvalidSegment, "{0} - Invalid segment - {1}", GetCurrentPosContext(), CurrentSegment);
                Errors.AddSegmentError(CurrentSegment, X12ErrorCode.UnexpectedSegmentCode
                    , X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnexpectedSegmentCode), SegmentNumber
                    , this.CurrentSegmentStartPos, this.CurrentSegmentEndPos-1, EdiErrorType.Error);
            }
            else
            {
                ValidTransactionSetCount++;
                // TODO: Optimization - Load DocumentPlug, reconstruct ISA and GA segment if transaction set type changed
                //if (PrevTransactionSetType != currentTransactionSetType)
                {
                    // Make sure that ISA and GA fields are already present
                    if (ISARecordFields == null || GSRecordFields == null)
                        throw new EDIReaderException(
                            string.Format("ISA and GA segments should be present before ST segment. {0}", GetCurrentPosContext()));

                    if (ISARecordFields.Length != MaxISAFieldRecordCount || GSRecordFields.Length == 0)
                        throw new EDIReaderException(
                            string.Format("ISA and GA segments length ({0}, {1}) does not match expected length ({2}, non-zero). {3}",
                                ISARecordFields.Length, GSRecordFields.Length, MaxISAFieldRecordCount, GetCurrentPosContext()));

                    //TODO: For testing invoking DocumentPlugFactory.CreateEDIDocumentPlug
                    if (DocumentPlug == null)
                    {
                        if (FPManager == null)
                            DocumentPlug = DocumentPlugFactory.CreateEDIDocumentPlug(currentTransactionSetType);
                        else
                            DocumentPlug = CreateEDIDocumentPlug(currentTransactionSetType, ISARecordFields);
                    }
                    else // Make sure that DocumentPlug and ST document type match
                    {
                        // DocumentPlug.DocumentType = 0 indicates that there was problem retrieving DocumentType
                        // while constructing DocumentPlug
                        if (DocumentPlug.DocumentType != 0 && DocumentPlug.DocumentType != currentTransactionSetType)
                        {
                            string errorDescription = "Spec Cert relates to document {0}, however ST01 value is {1}.; test File is rejected";
                            errorDescription = string.Format(errorDescription, DocumentPlug.DocumentType, currentTransactionSetType);

                            FieldError fieldError = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCodeValueCode, errorDescription, CurrentSegmentDetails[0]);

                            long currentSegmentFieldStartIndex = this.CurrentSegmentStartPos + 3; // length of "ST<delimiter>"
                            long currentSegmentFieldEndIndex = currentSegmentFieldStartIndex + CurrentSegmentDetails[1].Length - 1;

                            Logger.Error(location, EventId.EDIReaderInvalidTransactionSetType, errorDescription);
                            Errors.AddFieldError(CurrentSegmentDetails[0], "ST01", fieldError.ErrorCode, fieldError.Description, SegmentNumber, 1,
                                CurrentSegmentDetails[1], currentSegmentFieldStartIndex,
                                currentSegmentFieldEndIndex, EdiErrorType.Error);

                            return false;
                        }
                    }

                    CurrentPluglet = DocumentPlug.RootPluglet;

                    CurrentPluglet.ResetCurrentOccurances();

                    // Construct start segment list
                    CurrentPluglet.InitializeStartSegmentList();

                    FatpipeDocumentInst.TransactionSetType = currentTransactionSetType;
                    if (CurrentSegmentDetails.Length > 2)
                        FatpipeDocumentInst.TransactionNumber = CurrentSegmentDetails[2];
                    FatpipeDocumentInst.DocumentPlug = DocumentPlug;
                    FatpipeDocumentInst.RootFragment = CurrentPluglet.ConstructDocumentFragment(null, null);

                    // Construct ISA node
                    //CreateAndAddNewSegment(EDIState.ISA.ToString(), ISARecordFields);

                    // Construct GS node
                    //CreateAndAddNewSegment(EDIState.GS.ToString(), GSRecordFields);
                    //GSSegmentProcessed = true;
                    //GSPluglet = CurrentPluglet;

                    PrevTransactionSetType = currentTransactionSetType;
                }
                //else
                //{
                //    // Move to GS node to start new segment
                //    CurrentPluglet = GSPluglet;

                //    // Remove previous TransactionSet
                //    if (FatpipeDocumentInst.RootFragment.Children != null)
                //    {
                //        IDocumentFragment transactionSetChild = FatpipeDocumentInst.RootFragment.Children.Any( c => c.Pluglet.Tag == "TransactionSet");
                //        FatpipeDocumentInst.RootFragment.Children.Remove(transactionSetChild);
                //    }

                //    // Set errors to null
                //    FatpipeDocumentInst.Errors = null;
                //}

                // Construct ST node
                CreateAndAddNewSegment(EDIState.ST.ToString(), CurrentSegmentDetails);
            }

            sw.Stop();
            Logger.Debug(location, "Stop - {0}. Elapsed time {1} ms", GetCurrentPosContext(), sw.ElapsedMilliseconds);

            return true;
        }

        /// <summary>
        /// Find corrosponding Pluglet for given segmentname and create new segment
        /// </summary>
        /// <param name="segmentName"></param>
        /// <param name="segmentDetails"></param>
        private void CreateAndAddNewSegment(string segmentName, string[] segmentDetails)
        {
            const string firstSegmentName = "ST";

            string errorMsgs = string.Empty;
            IPluglet nextPluglet;

            string location = "EDIReader.CreateAndAddNewSegment";
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

                Logger.Error("EDIReader.CreateAndAddNewSegment", EventId.EDIReaderMissingMandatorySegment, "{0} - {1}", GetCurrentPosContext(), error);

                EdiErrorType errorType = nextPluglet.IsIgnore ? EdiErrorType.Warning : EdiErrorType.Error;

                foreach (string segment in missingMandatorySegments.Split(','))
                {
                    Errors.AddSegmentError(segmentName, X12ErrorCode.MandatorySegmentMissingCode
                        , string.Format("{0} : {1}", X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.MandatorySegmentMissingCode), segment)
                        , SegmentNumber , this.CurrentSegmentStartPos, this.CurrentSegmentEndPos-1, errorType);
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

                //  IPluglet x = new Pluglet("child"+i, "Unknown Data", PlugletType.Data, unknown);
                    //unknown.Children.Add(x);
                //}

                //DocumentFragment newFragment = unknown.ConstructDocumentFragment(segmentDetails, false, EDIDelimiters, out errors);

                errorMsgs = string.Format("{0} segment not found in schema after {1}", segmentName, CurrentPluglet.Name);

                Errors.AddSegmentError(segmentName, X12ErrorCode.UnrecognizedSegmentIDCode
                    , X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnrecognizedSegmentIDCode), SegmentNumber
                    , this.CurrentSegmentStartPos, this.CurrentSegmentEndPos-1, EdiErrorType.Error);

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
                        SequenceNumber = SegmentNumber,
                        StartOffset = this.CurrentSegmentStartPos,
                        EndOffset = this.CurrentSegmentEndPos-1,
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
                        , SegmentNumber, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, EdiErrorType.Error);
                }
                else
                {
                    DocumentFragment newFragment = nextPluglet.ConstructDocumentFragment(segmentDetails, false, EDIDelimiters, SegmentNumber,
                        this.CurrentSegmentStartPos, this.CurrentSegmentEndPos - 1, ref errors, out errorMsgs);

                    if (newFragment == null)
                    {
                        //errorMsgs = string.Format("{0} DocumentFragment creation failed. Errors: {1}", segmentName, errorMsgs);
                        Logger.Error(location, EventId.EDIReaderDocFragmentCreation, "{0} - {1}", GetCurrentPosContext(), errorMsgs);
                        // TODO: Replace UnexpectedSegmentCode with appropriate one
                        Errors.AddGenericError(segmentName, X12ErrorCode.UnexpectedSegmentCode, errorMsgs, SegmentNumber, this.CurrentSegmentStartPos, this.CurrentSegmentEndPos);
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
                if(FatpipeDocumentInst.Errors == null)
                    FatpipeDocumentInst.Errors = new List<string>();
                FatpipeDocumentInst.Errors.Add(errorMsgs);
            }
        }

        private string[] ReadNextSegment()
        {
            string segment = DocumentReader.ReadLine((char)EDIDelimiters.SegmentDelimiter, true);

            if (string.IsNullOrEmpty(segment))
                return null;

            CurrentSegment = segment;

            this.CurrentSegmentStartPos = this.CurrentSegmentEndPos == 0 ? 0 : this.CurrentSegmentEndPos + 1;
            if(SegmentDelimiter.Length == FormattedSegmentDelimiter.Length)
                this.CurrentSegmentEndPos = this.CurrentSegmentStartPos + (segment.Length + EDIDelimiters.SegmentDelimiterLength - 1); // segment delimiter has variable length due to optional CR LF
            else
                this.CurrentSegmentEndPos = this.CurrentSegmentStartPos + (segment.Length + FormattedSegmentDelimiter.Length - 1);

            string format = "Segment: {0}, StartPos: {1}, EndPos {2}";
            format = string.Format(format, SegmentNumberFromStart, CurrentSegmentStartPos, CurrentSegmentEndPos);
            //Console.WriteLine(format);
            SegmentNumberFromStart++;
            SegmentNumber++;

            return segment.Split((char)EDIDelimiters.FieldSeperator);
        }

        private void MoveToSESegment()
        {
            string[] segmentDetails = null;

            do
            {
                segmentDetails = ReadNextSegment();
                if (segmentDetails != null && segmentDetails.Length > 0)
                {
                    TransactionSegment = TransactionSegment + SegmentDelimiter + CurrentSegment;
                    FormattedTransactionSegment = FormattedTransactionSegment + FormattedSegmentDelimiter + CurrentSegment;
                }
            } while (segmentDetails != null && string.Compare("SE", segmentDetails[0], true) != 0);
        }

        private EDIState GetEDIState(string firstField)
        {
            if (string.IsNullOrEmpty(firstField))
                throw new ArgumentNullException(firstField);

            EDIState state;

            //TODO: How many valid states EDI has? Should we validate it here?
            if (Enum.TryParse(firstField, out state) == false)
                state = EDIState.Other;

            return state;
        }

        private string GetCurrentPosContext()
        {
            return string.Format("Segment#:{0}", SegmentNumberFromStart);
        }

        private IDocumentPlug CreateEDIDocumentPlug(int currentTransactionSetType, string[] ISARecordFields)
        {
            string targetNamespace, name;
            IDocumentPlug documentPlug = null;

            switch (currentTransactionSetType)
            {
                case 820:
                    name = "X12_005010X306_820R1";
                    targetNamespace = @"urn:x12:schemas:005010X306:820R1:HealthInsuranceExchangeRelatedPayments";

                    if (SchemaCache.ContainsKey(name))
                        documentPlug = SchemaCache[name];
                    break;
                
                case 850:
                    name = "X12_00401_850";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/X12/2006";

                    if (SchemaCache.ContainsKey(name))
                        documentPlug = SchemaCache[name];
                    break;

                case 277:
                    name = "X12_005010X214_277B3";
                    //targetNamespace = @"urn:x12:schemas:005:010:277B3:HealthCareInformationStatusNotification";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/X12/2006";

                    if (SchemaCache.ContainsKey(name))
                        documentPlug = SchemaCache[name];
                    break;

                case 810:
                    name = "X12_00501_810";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/X12/2006";

                    if (SchemaCache.ContainsKey(name))
                        documentPlug = SchemaCache[name];
                    break;

                default:
                    throw new PlugDataModelException(string.Format("{0} schema not found", currentTransactionSetType));
            }

            if (documentPlug == null)
            {
                string docType = string.Format("{0}#{1}", targetNamespace, name);
                Stream schemaStream = FPManager.RetrieveSchema(docType);
                XmlReader reader = new XmlTextReader(schemaStream);

                //Stream schemaStream = FatPipeManager.RetrieveSchema(rootNodeName);
                //XmlReader reader = new XmlTextReader(schemaStream);

                XmlSchemaCollection schemaCollection = new XmlSchemaCollection();
                schemaCollection.Add(targetNamespace, reader);

                documentPlug = DocumentPlugFactory.CreateDocumentPlugFromXmlSchema(schemaCollection, targetNamespace, name);

                SchemaCache[name] = documentPlug;
             }

            return documentPlug;
        }

        private void ValidateContingencies()
        {
            // Assumption: If id data segment is not present then this function will not report error
            // as we must have already reported that data segment as missing segment earlier
            Dictionary<string, List<IDocumentFragment>> contingencyOccurances = new Dictionary<string, List<IDocumentFragment>>();
            ReadSegmentsWithIdDataTypeChildrens(contingencyOccurances, FatpipeDocumentInst.RootFragment);

            foreach (string path in contingencyOccurances.Keys)
            {
                // Select pluglet for current path
                // Selecting pluglet from the 1st documentFragment as pluglet will be
                // same for all documentFragments with same path
                IDocumentFragment documentFragment = contingencyOccurances[path][0];
                IPluglet pluglet = documentFragment.Pluglet;

                // Pluglet will point to segment, process all child with data type as X12_IdDataType
                // Filter out non-mandatory data type pluglets
                foreach (Pluglet child in pluglet.Children)
                {
                    if (child.PlugletType == PlugletType.Data && child.DataType is X12_IdDataType && child.IsMandatory == true)
                    {
                        List<string> presentValues = GetAllPresentValues(contingencyOccurances[path], child);

                        X12_IdDataType dataType = child.DataType as X12_IdDataType;
                        foreach (string allowedValue in dataType.AllowedValues.Keys)
                        {
                            Contingency contingencies = dataType.GetContingencies(allowedValue);

                            // TODO: Use Ignore flag at id value level

                            // If Id value does not have any contingency then segment with that value must exist
                            if (contingencies == null || contingencies.ContingencyValues.Count == 0)
                            {
                                if (presentValues.Contains(allowedValue) == false && dataType.IsOptionalValue(allowedValue) == false)
                                {
                                    Errors.AddSegmentError(pluglet.Tag, X12ErrorCode.DeMandatoryIdValueMissingCode
                                        , string.Format("{0} : {1}", X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeMandatoryIdValueMissingCode), allowedValue)
                                        , documentFragment.SequenceNumber, documentFragment.StartOffset, documentFragment.EndOffset, EdiErrorType.Error);
                                }
                            }
                            // If Id value has contingencies of type Enumeration then segment with that value or any contingency value must exist
                            else if(contingencies.Type == ContingencyType.Enumeration)
                            {
                                bool valuePresent = presentValues.Contains(allowedValue);
                                if(valuePresent == false)
                                {
                                    foreach(string alternateValue in contingencies.ContingencyValues)
                                    {
                                        valuePresent = presentValues.Contains(alternateValue);
                                        if(valuePresent)
                                            break;
                                    }
                                }

                                if (valuePresent == false)
                                {
                                    Errors.AddSegmentError(pluglet.Tag, X12ErrorCode.DeMandatoryIdValueOrAlternativeValueMissingCode
                                        , string.Format("{0} : {1}", X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeMandatoryIdValueOrAlternativeValueMissingCode), allowedValue)
                                        , documentFragment.SequenceNumber, documentFragment.StartOffset, documentFragment.EndOffset, EdiErrorType.Error);
                                }
                            }
                            // If contingency type is cross segment then either both values must exist or both values missing
                            else if (contingencies.Type == ContingencyType.CrossSegment)
                            {
                                // TODO: handle all values in contingencies.ContingencyValues
                                string xPath = contingencies.ContingencyValues[0];
                                bool currentValuePresent = presentValues.Contains(allowedValue);
                                bool crossSegmentValuePresent = IsCrossSegmentValuePresent(contingencyOccurances, xPath, pluglet.PathSeperator);

                                if (currentValuePresent != crossSegmentValuePresent)
                                {
                                    Errors.AddSegmentError(pluglet.Tag, X12ErrorCode.DeCrossSegmentIdValueOccurancesDoesNotMatch
                                        , string.Format("{0} : {1} {2}", X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeCrossSegmentIdValueOccurancesDoesNotMatch), allowedValue, xPath)
                                        , documentFragment.SequenceNumber, documentFragment.StartOffset, documentFragment.EndOffset, EdiErrorType.Error);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsCrossSegmentValuePresent(Dictionary<string, List<IDocumentFragment>> contingencyOccurances, string xPath, string pathSeparator)
        {
            string enumCode;
            string dataElementTag;
            string segmentPath;
            bool valuePresent = false;
            int enumCodeStartPos = xPath.IndexOf("[");
            int dataElementTagPos = xPath.LastIndexOf(pathSeparator);

            enumCode = xPath.Substring(enumCodeStartPos + 1, xPath.Length - enumCodeStartPos - 2);
            dataElementTag = xPath.Substring(dataElementTagPos + pathSeparator.Length, enumCodeStartPos - dataElementTagPos - pathSeparator.Length);
            segmentPath = xPath.Substring(0, dataElementTagPos);

            List<IDocumentFragment> contingencyDocumentFragments = null;
            if (contingencyOccurances.TryGetValue(segmentPath, out contingencyDocumentFragments))
            {
                foreach (IDocumentFragment documentFragment in contingencyDocumentFragments)
                {
                    // go to data element matching dataElementTag in contingency segment
                    foreach (IDocumentFragment dataSegment in documentFragment.Children)
                    {
                        if (string.Compare(dataSegment.Pluglet.Tag, dataElementTag, true) == 0)
                        {
                            if (string.Compare(dataSegment.Value, enumCode, 0) == 0)
                            {
                                valuePresent = true;
                                break;
                            }
                        }
                    }
                    if (valuePresent == true)
                        break;
                }
            }

            return valuePresent;
        }

        private List<string> GetAllPresentValues(List<IDocumentFragment> documentFragments, Pluglet pluglet)
        {
            List<string> presentValues = new List<string>();

            foreach (DocumentFragment documentfragment in documentFragments)
            {
                foreach (DocumentFragment child in documentfragment.Children)
                {
                    if (string.Compare(child.Pluglet.Tag, pluglet.Tag, true) == 0)
                    {
                        presentValues.Add(child.Value);
                        break;
                    }
                }
            }

            return presentValues;
        }

        private void ReadSegmentsWithIdDataTypeChildrens(Dictionary<string, List<IDocumentFragment>> contingencyOccurances, IDocumentFragment documentFragment)
        {
            if (documentFragment == null 
                || documentFragment.Pluglet == null 
                || documentFragment.Pluglet.PlugletType == PlugletType.Data
                || documentFragment.Pluglet.PlugletType == PlugletType.Unknown)
                return;

            bool currentSegmentAdded = false;

            if(documentFragment.Children != null && documentFragment.Children.Count > 0)
            {
                foreach (IDocumentFragment child in documentFragment.Children)
                {
                    if (child.Pluglet != null)
                    {
                        if(child.Pluglet.PlugletType == PlugletType.Data)
                        {
                            if(child.Pluglet.DataType is X12_IdDataType && child.Pluglet.IsMandatory == true && currentSegmentAdded == false)
                            {
                                List<IDocumentFragment> contingencies;
                                if (contingencyOccurances.TryGetValue(documentFragment.Pluglet.Path, out contingencies) == false)
                                {
                                    contingencies = new List<IDocumentFragment>();
                                    contingencyOccurances.Add(documentFragment.Pluglet.Path, contingencies);
                                }
                                contingencies.Add(documentFragment);

                                currentSegmentAdded = true;
                            }
                        }
                        else
                            ReadSegmentsWithIdDataTypeChildrens(contingencyOccurances, child);
                    }
                }
            }
        }

        #endregion
    }
}
