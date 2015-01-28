using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using Maarg.Fatpipe.LoggingService;
using System.Diagnostics;
using Maarg.Contracts;
using System.Xml;
using System.Xml.Schema;

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

        private static readonly ILogger Logger = LoggerFactory.Logger;

        #region Properties
        private IFatpipeManager FPManager { get; set; }

        public Delimiters EDIDelimiters { get; set; }

        private FatpipeDocument FatpipeDocumentInst { get; set; }

        private StreamReader DocumentReader { get; set; }
        private IDocumentPlug DocumentPlug { get; set; }
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
        private int CurrentSegmentNumber { get; set; }
        private int ValidTransactionSetCount { get; set; }
        private int InvalidTransactionSetCount { get; set; }

        // Properties to store original payload
        private string ISASegment { get; set; }
        private string GSSegment { get; set; }
        private string TransactionSegment { get; set; }
        private string SegmentDelimiter { get; set; }
        #endregion

        #region IEDIReader Methods
        /// <summary>
        /// Initialize EDIReader with a stream. Verify that stream contains EDI document (ISA segment).
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>true if EDI document otherwise false</returns>
        public bool Initialize(Stream stream, IFatpipeManager fatpipeManager)
        {
            Logger.Info("EDIReader.Initialize", "Start");
            
            if (stream == null)
                throw new ArgumentNullException("stream");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            FPManager = fatpipeManager;

            long orgStartPos = stream.Position;

            Delimiters delimiters;
            InterchangeTokenizer tokenizer = new InterchangeTokenizer(stream);
            bool isValidEDIDocument = tokenizer.IsX12Interchange(out delimiters);

            CurrentSegmentNumber = 0;
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

                CurrentSegmentNumber = 1;

                this.DocumentReader = new StreamReader(stream, Encoding.UTF8);
                //TODO: Why is seek required here?
                this.DocumentReader.BaseStream.Seek(tokenizer.ISARecordLen + orgStartPos, SeekOrigin.Begin);
                FatpipeDocumentInst = new FatpipeDocument();

                SegmentDelimiter = ((char)EDIDelimiters.SegmentDelimiter).ToString();
            }
            else
                ISASegment = string.Empty;

            LastState = EDIState.ISA;

            sw.Stop();
            Logger.Info("EDIReader.Initialize", "Stop. Elapsed time {0} ms", sw.ElapsedMilliseconds);

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

            Logger.Info(location, "Start - {0}", GetCurrentPosContext());

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

                        ProcessSTSegment();

                        TransactionSegment = CurrentSegment;
                        break;

                    case EDIState.SE:
                        if (!(LastState == EDIState.ST || LastState == EDIState.Other))
                            throw new EDIReaderException(string.Format("{0} segment should appear only after ST or Other. {1}", LastState, GetCurrentPosContext()));

                        CheckForMissingSegments(CurrentSegmentDetails[0]);

                        //TODO: Add validation of SE segment details values

                        isTransactionSetFinished = true;

                        TransactionSegment = TransactionSegment + SegmentDelimiter + CurrentSegment;
                        break;

                    case EDIState.Other:
                        if (!(LastState == EDIState.ST || LastState == EDIState.Other))
                            throw new EDIReaderException(string.Format("{0} segment should appear only after ST or Other. {1}", LastState, GetCurrentPosContext()));

                        CreateAndAddNewSegment(CurrentSegmentDetails[0], CurrentSegmentDetails);

                        TransactionSegment = TransactionSegment + SegmentDelimiter + CurrentSegment;
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
            }

            sw.Stop();
            Logger.Info(location, "Stop - {0}. Elapsed time {1} ms", GetCurrentPosContext(), sw.ElapsedMilliseconds);

            return isTransactionSetFinished == true ? FatpipeDocumentInst : null;
        }

        /// <summary>
        /// Return GS 3rd data segment value
        /// </summary>
        /// <returns></returns>
        public string GetReceiverId()
        {
            if(GSRecordFields == null || GSRecordFields.Length < 4)
                return null;

            return GSRecordFields[3];
        }

        /// <summary>
        /// Return GS 2nd data segment value
        /// </summary>
        /// <returns></returns>
        public string GetSenderId()
        {
            if (GSRecordFields == null || GSRecordFields.Length < 4)
                return null;

            return GSRecordFields[2];
        }


        #endregion

        #region Private Methods

        private void ProcessSTSegment()
        {
            string location = "EDIReader.ProcessSTSegment";
            string errors = string.Empty;

            Logger.Info(location, "Start - {0}", GetCurrentPosContext());
            Stopwatch sw = new Stopwatch();
            sw.Start();

            LastState = EDIState.ST;
            int currentTransactionSetType;

            if (CurrentSegmentDetails == null || CurrentSegmentDetails.Length < 2
                || int.TryParse(CurrentSegmentDetails[1], out currentTransactionSetType) == false)
            {
                InvalidTransactionSetCount++;

                //TODO: Add error
                Logger.Error(location, "{0} - Invalid segment - {1}", GetCurrentPosContext(), CurrentSegment);
            }
            else
            {
                // TODO: Optimization - Load DocumentPlug, reconstruct ISA and GA segment if transaction set type changed
                //if (PrevTransactionSetType != currentTransactionSetType)
                {
                    // Make sure that ISA and GA fields are already present
                    if (ISARecordFields == null || GSRecordFields == null)
                        throw new EDIReaderException(
                            string.Format("ISA and GA segments should be present before ST segment. {0}", GetCurrentPosContext()));

                    if (ISARecordFields.Length != MaxISAFieldRecordCount || GSRecordFields.Length == 0)
                        throw new EDIReaderException(
                            string.Format("ISA and GA segments length ({0}, {1}) does not match expected length ({2}, non-zero). {4}",
                                ISARecordFields.Length, GSRecordFields.Length, MaxISAFieldRecordCount, GetCurrentPosContext()));

                    //TODO: For testing invoking DocumentPlugFactory.CreateEDIDocumentPlug
                    if(FPManager == null)
                        DocumentPlug = DocumentPlugFactory.CreateEDIDocumentPlug(currentTransactionSetType, ISARecordFields);
                    else
                        DocumentPlug = CreateEDIDocumentPlug(currentTransactionSetType, ISARecordFields);

                    CurrentPluglet = DocumentPlug.RootPluglet;

                    // Construct start segment list
                    CurrentPluglet.InitializeStartSegmentList();

                    FatpipeDocumentInst.DocumentPlug = DocumentPlug;
                    FatpipeDocumentInst.RootFragment = CurrentPluglet.ConstructDocumentFragment(null, null);

                    // Construct ISA node
                    CreateAndAddNewSegment(EDIState.ISA.ToString(), ISARecordFields);

                    // Construct GS node
                    CreateAndAddNewSegment(EDIState.GS.ToString(), GSRecordFields);
                    GSSegmentProcessed = true;
                    GSPluglet = CurrentPluglet;

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
            Logger.Info(location, "Stop - {0}. Elapsed time {1} ms", GetCurrentPosContext(), sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Find corrosponding Pluglet for given segmentname and create new segment
        /// </summary>
        /// <param name="segmentName"></param>
        /// <param name="segmentDetails"></param>
        private void CreateAndAddNewSegment(string segmentName, string[] segmentDetails)
        {
            string errors = string.Empty;
            IPluglet nextPluglet;

            string location = "EDIReader.CreateAndAddNewSegment";
            Logger.Debug(location, "Adding {0} segment", segmentName);

            nextPluglet = CheckForMissingSegments(segmentName);

            if (nextPluglet == null)
            {
                //TODO: Revisit following if condition - do we really want to ignore ISA/GS segment missing in schema?
                if (segmentName != "ISA" && segmentName != "GS")
                {
                    errors = string.Format("{0} segment not found in schema after {1}", segmentName, CurrentPluglet.Name);

                    Logger.Error(location, "{0} - {1}", GetCurrentPosContext(), errors);
                }
            }
            else
            {
                DocumentFragment newFragment = nextPluglet.ConstructDocumentFragment(segmentDetails, false, EDIDelimiters, out errors);

                if (newFragment == null)
                {
                    errors = string.Format("{0} DocumentFragment creation failed. Errors: {1}", segmentName, errors);
                    Logger.Error(location, "{0} - {1}", GetCurrentPosContext(), errors);
                }
                else
                {
                    ((DocumentFragment)FatpipeDocumentInst.RootFragment).AddDocumentFragment(newFragment);

                    CurrentPluglet = newFragment.Pluglet;
                }
            }

            if (!string.IsNullOrEmpty(errors))
            {
                if(FatpipeDocumentInst.Errors == null)
                    FatpipeDocumentInst.Errors = new List<string>();
                FatpipeDocumentInst.Errors.Add(errors);
            }
        }

        private IPluglet CheckForMissingSegments(string segmentName)
        {
            IPluglet nextPluglet, startFromPluglet;
            string missingMandatorySegments = string.Empty;

            startFromPluglet = CurrentPluglet;
            // Check if CurrentPluget is segment pluglet which was already
            // processed, if this is the case then check for repeatationInfo
            if (startFromPluglet.PlugletType == PlugletType.Segment)
            {
                // TODO: Correct "startFromPluglet.RepetitionInfo.MaxOccurs > 1" as this needs to consider already constructed segment count
                if (startFromPluglet.IsRepeatable == false || startFromPluglet.RepetitionInfo.MaxOccurs == 1)
                    startFromPluglet = startFromPluglet.GetNextPluglet();
            }

            nextPluglet = startFromPluglet.Find(segmentName, startFromPluglet.IsMandatory, ref missingMandatorySegments);

            // Commented out following check since we want to proceed to get all errors
            // even if some segment is invalid
            //if (nextPluglet == null)
            //    throw new EDIReaderException(string.Format("{0} pluglet not found.", segmentName));

            // Add missing mandatory segment list only if node is found
            if (nextPluglet != null && !string.IsNullOrEmpty(missingMandatorySegments))
            {
                string error = string.Format("Missing mandatory segment list during constructing {0} segment: {1}"
                                    , segmentName, missingMandatorySegments);

                if (FatpipeDocumentInst.Errors == null)
                    FatpipeDocumentInst.Errors = new List<string>();
                FatpipeDocumentInst.Errors.Add(error);

                Logger.Error("EDIReader.CheckForMissingSegments", "{0} - {1}", GetCurrentPosContext(), error); 
            }
            return nextPluglet;
        }

        private string[] ReadNextSegment()
        {
            string segment = DocumentReader.ReadLine((char)EDIDelimiters.SegmentDelimiter, true);

            if (string.IsNullOrEmpty(segment))
                return null;

            CurrentSegment = segment;

            CurrentSegmentNumber++;

            return segment.Split((char)EDIDelimiters.FieldSeperator);
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
            return string.Format("Segment#:{0}", CurrentSegmentNumber);
        }

        private IDocumentPlug CreateEDIDocumentPlug(int currentTransactionSetType, string[] ISARecordFields)
        {
            string path, targetNamespace, name;
            IDocumentPlug documentPlug = null;

            switch (currentTransactionSetType)
            {
                case 850:
                    name = "X12_00401_850";
                    targetNamespace = @"http://schemas.microsoft.com/BizTalk/EDI/X12/2006";

                    if (SchemaCache.ContainsKey(name))
                        documentPlug = SchemaCache[name];
                    break;

                case 277:
                    name = "X12_005010X214_277B3";
                    targetNamespace = @"urn:x12:schemas:005:010:277B3:HealthCareInformationStatusNotification";

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

        #endregion
    }
}
