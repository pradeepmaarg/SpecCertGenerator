using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace Maarg.Fatpipe.Plug.DataModel
{
    ///
    /// This class represents all errors in an interchange
    ///
    public class InterchangeErrors
    {
        EdiSectionErrors isaIeaErrors;
        IList<FunctionalGroupErrors> functionalGroupErrors; //of FunctionalGroupErrorInfo

        public InterchangeErrors()
        {
            isaIeaErrors = new EdiSectionErrors();
            functionalGroupErrors = new List<FunctionalGroupErrors>();
        }

        #region Properties
        public EdiSectionErrors IsaIeaErrorList
        {
            get { return isaIeaErrors; }
        }

        public IList<FunctionalGroupErrors> FunctionalGroupErrors
        {
            get { return functionalGroupErrors; }
        }

        public int Count
        {
            get
            {
                int count = isaIeaErrors.Count;
                foreach (FunctionalGroupErrors errorInfo in functionalGroupErrors)
                {
                    count += errorInfo.Count;
                }

                return count;
            }
        }
        #endregion

        public FunctionalGroupErrors CreateNewFunctionalGroupErrorInfo(int sequenceNo, string idCode, string ctrlNumber)
        {
            FunctionalGroupErrors info = new FunctionalGroupErrors(sequenceNo, idCode, ctrlNumber);
            functionalGroupErrors.Add(info);
            return info;
        }
    }

    ///
    /// This class represents all errors in a functional group
    ///
    public class FunctionalGroupErrors
    {
        int sequenceNo;
        string functionalIdCode;
        string controlNumber;
        EdiSectionErrors gsGeErrors;
        IList<TransactionSetErrors> transactionSetErrors;
        int countOfContainedTS;
        int countOfAcceptedTS;

        public FunctionalGroupErrors(int seqNo, string idCode, string ctrlNumber)
        {
            sequenceNo = seqNo;
            functionalIdCode = idCode;
            controlNumber = ctrlNumber;
            gsGeErrors = new EdiSectionErrors();
            transactionSetErrors = new List<TransactionSetErrors>();
            countOfContainedTS = countOfAcceptedTS = 0;
        }

        #region Properties
        public int CountOfContainedTS
        {
            get { return countOfContainedTS; }
            set { countOfContainedTS = value; }
        }

        public int CountOfAcceptedTS
        {
            get { return countOfAcceptedTS; }
            set { countOfAcceptedTS = value; }
        }

        public string FunctionalIdCode
        {
            get { return functionalIdCode; }
            set { functionalIdCode = value; }
        }

        public string ControlNumber
        {
            get { return controlNumber; }
            set { controlNumber = value; }
        }


        

        public EdiSectionErrors GsGeErrorList
        {
            get { return gsGeErrors; }
        }

        public IList<TransactionSetErrors> TransactionSetErrors
        {
            get { return transactionSetErrors; }
        }

        public int SequenceNo
        {
            get { return sequenceNo; }
        }

        public int Count
        {
            get
            {
                int count = gsGeErrors.Count;
                foreach (TransactionSetErrors errorInfo in transactionSetErrors)
                {
                    count += errorInfo.ErrorCount;
                }

                return count;
            }
        }
        #endregion

        public void AddTransactionSetErrorInfo(TransactionSetErrors tsError)
        {
            transactionSetErrors.Add(tsError);
        }
    }

    public class TransactionSetErrors
    {
        int sequenceNo;  //sequential count of the TS in the current group

        string mIdCode; //more meaningful for X12
        string controlNumber; //more meaningful for X12
        string mImplementationCode; // only meaningful for X12 (ST03)
        EdiSectionErrors mTsErrorList;

        public TransactionSetErrors(int seqNo, string code, string number, EdiSectionErrors list)
        {
            sequenceNo = seqNo;
            mIdCode = code;
            controlNumber = number;
            mTsErrorList = list;
        }

        #region Properties
        public string IdCode
        {
            get { return mIdCode; }
            set { mIdCode = value; }
        }

        public string ImplementationIdCode
        {
            get { return mImplementationCode; }
            set { mImplementationCode = value; }
        }

        public string ControlNumber
        {
            get { return controlNumber; }
            set { controlNumber = value; }
        }

        public int SequenceNo
        {
            get { return sequenceNo; }
        }

        public EdiSectionErrors TsErrorList
        {
            get { return mTsErrorList; }
        }

        public int ErrorCount
        {
            get { return mTsErrorList != null ? mTsErrorList.Count : 0; }
        }
        #endregion

        public void WriteError(StringBuilder sb, ref int errorIndex, bool bWriteHeader)
        {
            if (bWriteHeader)
            {
                sb.Append("TransationSetError");
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);

                sb.Append("SequenceNo");
                sb.Append(this.SequenceNo);
                sb.Append(Environment.NewLine);

                sb.Append("TsIdCode");
                sb.Append(this.IdCode);
                sb.Append(Environment.NewLine);

                sb.Append("ControlNumber");
                sb.Append(this.ControlNumber);
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }
        }
    }

    /*
     * This class represents errors that happen in a unit of
     * message processing like parsing or serializing.
     * 
     * Errors happen in various places
     * 
     * 1) Processing of Envelope header and trailer
     * 2) Processing of a functional group
     * 3) Processing of transaction set inside a functional group
     */
    public class EdiSectionErrors
    {
        IList<GenericError> mGenericErrorList; //of GenericError objects
        IList<SegmentError> mSegmentErrorList; //of SegmentError objects
        IList<FieldError> mFieldErrorList;

        public EdiSectionErrors()
        {
            mGenericErrorList = new List<GenericError>();
            mSegmentErrorList = new List<SegmentError>();
            mFieldErrorList = new List<FieldError>();
        }

        #region Properties
        public IList<GenericError> GenericErrorList
        {
            get { return mGenericErrorList; }
        }

        public IList<SegmentError> SegmentErrorList
        {
            get { return mSegmentErrorList; }
        }

        public IList<FieldError> FieldErrorList
        {
            get { return mFieldErrorList; }
        }

        public int Count
        {
            get
            {
                return mGenericErrorList.Count + mSegmentErrorList.Count + mFieldErrorList.Count;
            }
        }
        #endregion

        public void WriteError(StringBuilder sb, ref int errorIndex)
        {
            foreach (GenericError nseError in this.GenericErrorList)
            {
                sb.Append("ErrorNo");
                sb.Append(errorIndex++);
                sb.Append("Nse");
                sb.Append(Environment.NewLine);

                sb.Append(nseError.ErrorCode);
                sb.Append("Quote");
                sb.Append(nseError.Description);
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }

            foreach (SegmentError seError in this.SegmentErrorList)
            {
                sb.Append("ErrorNo");
                sb.Append(errorIndex++);
                sb.Append("Se");
                sb.Append(Environment.NewLine);

                sb.Append("SegmentID");
                sb.Append(seError.SegmentID);
                sb.Append(Environment.NewLine);

                sb.Append("PositionInTS");
                sb.Append(seError.PositionInTS);
                sb.Append(Environment.NewLine);

                sb.Append(seError.ErrorCode);
                sb.Append("Quote");
                sb.Append(seError.Description);
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
                
            }

            foreach (FieldError fieldError in mFieldErrorList)
            {
                sb.Append("ErrorNo");
                sb.Append(errorIndex++);
                sb.Append("Fe");
                sb.Append(Environment.NewLine);

                //sb.Append("SegmentID");
                //sb.Append(seError.SegmentID);
                //sb.Append(Environment.NewLine);

                //sb.Append("PositionInTS");
                //sb.Append(seError.PositionInTS);
                //sb.Append(Environment.NewLine);

                sb.Append("DataElementID");
                sb.Append(fieldError.XmlTag);
                sb.Append(Environment.NewLine);

                //sb.Append(EdiConstants.Indent);
                sb.Append("PositionInSegment");
                sb.Append(fieldError.PositionInSegment);
                sb.Append(Environment.NewLine);

                if (fieldError.PositionInField > 0)
                {
                    sb.Append("PositionInField");
                    sb.Append(fieldError.PositionInField);
                    sb.Append(Environment.NewLine);
                }

                sb.Append("DataValue");
                sb.Append(fieldError.DataValue);
                sb.Append(Environment.NewLine);

                sb.Append(fieldError.ErrorCode);
                sb.Append("Quote");
                sb.Append(fieldError.Description);
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }
        }
    }

    public class SchemaValidationError : EdiError
    {
        //XPath of the node where the error occured.
        //Would be blank if the error is not node related
        protected string mNodeXPath;

        public SchemaValidationError(int code, string desc, EdiErrorType errorType, string xpath)
            : base(code, desc, -1, errorType, -1, -1)
        {
            mNodeXPath = xpath;
        }

        public string NodeXPath
        {
            get { return mNodeXPath; }
            set { mNodeXPath = value; }
        }
    }

    public enum EdiErrorType
    {
        Warning,
        Error
    }

    public class EdiError
    {
        int mPositionInTS;

        public EdiError(int code, string desc, int segNo, EdiErrorType errorType, long startIndex, long endIndex)
        {
            ErrorCode = code;
            Description = desc;
            EdiErrorType = errorType;
            StartIndex = startIndex;
            EndIndex = endIndex;
            mPositionInTS = segNo;
        }

        #region Properties
        public int ErrorCode { get; set; }

        public string Description { get; private set; }

        public EdiErrorType EdiErrorType { get; private set; }

        public long StartIndex { get; private set; }
        public long EndIndex { get; private set; }
        public int PositionInTS
        {
            get { return mPositionInTS; }
        }
        #endregion
    }
   
    public class GenericError : EdiError
    {
        protected string mApproxSegmentTag;

        public GenericError(int code, string desc, int sequenceNo)
            : this(code, desc, sequenceNo, null)
        {
        }

        public GenericError(int code, string desc, int sequenceNo, string tag)
            : this(code, desc, sequenceNo, tag, -1, -1)
        {
            mApproxSegmentTag = tag;
        }

        public GenericError(int code, string desc, int sequenceNo, string tag, long startIndex, long endIndex)
            : base(code, desc, sequenceNo, EdiErrorType.Error, startIndex, endIndex)
        {
            mApproxSegmentTag = tag;
        }

        public string ApproxSegmentTag
        {
            get { return mApproxSegmentTag; }
        }
    }

    /*
     * This class represents a Segment level error
     */
    public class SegmentError : EdiError
    {
        protected string mSegmentID;
        
        string mExplicitLoopID;

        public SegmentError(string segID, int sequenceNo,
            int code, string desc, string explicitLoopID,
            long startIndex, long endIndex, EdiErrorType errorType)
            : base(code, desc, sequenceNo, errorType, startIndex, endIndex)
        {
            mSegmentID = segID;
            mExplicitLoopID = explicitLoopID;
        }

        #region Properties
        public string ExplicitLoopID
        {
            get { return mExplicitLoopID; }
        }

        public string SegmentID
        {
            get { return mSegmentID; }
        }

        
        #endregion
    }

    public class FieldError : EdiError
    {
        protected int mPositionInSegment;
        protected int mPositionInField; //applicable only to sub-component, ohtherwise -1
        protected int mRepetitionCount; //repetition count of the field
        protected string mDataValue;
        protected string mReferenceDesignator;
        protected string mXmlTag;

        public FieldError(string fieldId, int positionInSegment, int positionInField, int repetitionCount,
            int code, string desc, int segmentNo, string val, string refDesignator)
            : this(fieldId, positionInSegment, positionInField, repetitionCount,
            code, desc, segmentNo, val, refDesignator, -1, -1, EdiErrorType.Error)
        {
        }

        public FieldError(string fieldId, int positionInSegment, int positionInField, int repetitionCount,
            int code, string desc, int segmentNo, string val, string refDesignator,
            long startIndex, long endIndex, EdiErrorType errorType)
            : base(code, desc, segmentNo, errorType, startIndex, endIndex)
        {
            FieldId = fieldId;
            mPositionInSegment = positionInSegment;
            mPositionInField = positionInField;
            mRepetitionCount = repetitionCount;
            mDataValue = val;
            mReferenceDesignator = refDesignator;
        }

        #region Properties
        public string XmlTag
        {
            get { return mXmlTag; }
            set { mXmlTag = value; }
        }

        public string ReferenceDesignator
        {
            get { return mReferenceDesignator; }
        }

        public int PositionInField
        {
            get { return mPositionInField; }
        }

        public int RepetitionCount
        {
            get { return mRepetitionCount; }
        }

        public int PositionInSegment
        {
            get { return mPositionInSegment; }
        }

        public string DataValue
        {
            get { return mDataValue; }
            set { mDataValue = value; }
        }

        public string FieldId { get; private set; }
        #endregion
    }
}