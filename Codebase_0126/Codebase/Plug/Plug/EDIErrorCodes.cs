using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class SchemaErrorCode
    {
        //Invalid document type {0}
        public const int SchemaCode100EInvalidDocType = 100;

        //Null schema {0}
        public const int SchemaCode101ENullSchema = 101;

        //Null root element
        public const int SchemaCode102ENullRootElement = 102;

        //Segment should have a name with atleast 2 characters
        public const int SchemaCode103EInvalidTagLength = 103;

        //Node contains attributes. Only elements are allowed
        public const int SchemaCode104ESchemaContainsAttribute = 104;

        //ContentModel should be blank
        public const int SchemaCode105EUnexpectedContentModelFound = 105;

        //XmlSchemaSequence not found under node
        public const int SchemaCode106EXmlSchemaGroupNotFound = 106;

        //Node contains an XmlSchemaAny element
        public const int SchemaCode107EXmlSchemaAnyFound = 107;

        //Node contains an XmlSchemaAll element
        public const int SchemaCode108EXmlSchemaAllFound = 108;

        //Node contains an XmlSchemaSequence element
        public const int SchemaCode109EXmlSchemaSequenceFound = 109;

        //Node contains an XmlSchemaChoice element
        public const int SchemaCode110EXmlSchemaChoiceFound = 110;

        //Node has Tag {0}, it's type differs from a previously defined element's type {1}
        public const int SchemaCode111EAmbiguousElementTypes = 111;

        //The control segment schema has some missing segments, ISA, IEA, GS, GE, ST, SE
        public const int SchemaCode112EControlSegmentMissing = 112;

        //ST and/or SE segments are missing
        public const int SchemaCode113EStOrSeMissing = 113;

        //Transaction set schema contains one or more of control segments ISA, IEA, GS, GE
        public const int SchemaCode114EOtherControlSegmentsPresent = 114;

        //Control schema should have segments in the following order: ISA GS ST .... SE GE IEA
        public const int SchemaCode115EControlSchemaSegmentsOutOfOrder = 115;

        //Schema should have segments in the following order ST .... SE
        public const int SchemaCode116ETransactionSetSchemaStSeOutOfOrder = 116;

        //Node has an invalid depth of {0}
        public const int SchemaCode117EInvalidDepth = 117;

        //Component can not repeat, it has an invalid repetition count of {0}
        public const int SchemaCode118EInvalidRepetition = 118;

        //Node has no child
        public const int SchemaCode119EEmptyNode = 119;

        //Node has invalid property specification. Properties need to be immediate children of Segment nodes
        public const int SchemaCode120EInvalidPropertySpec = 120;

        //Subject name {0} not found in segment
        public const int SchemaCode121ESubjectNameNotFoundInRule = 121;

        //Child number {0} of segment {1} is not a Simple Field
        public const int SchemaCode122EChildIsNotASimpleOne = 122;

        //TA1 schema does not contain TA1 segment
        public const int SchemaCode123ETA1SegmentNotPresent = 123;

        //The schema can not be compacted as it contains references to other schemas
        public const int SchemaCode124ESchemaContainsReferencedSchemas = 124;

        //The loop contains a leaf node. It can only contain other Loops or Segments
        public const int SchemaCode125ELoopContainsLeafNode = 125;

        //XmlSchemaAll found under node and parent is not All
        public const int SchemaCode126EXmlSchemaAllParentAllNotFound = 126;

        //Unsupported Loop Type Detected
        public const int SchemaCode127UnsuppoetedLoopTypeDetected = 127;

        //XmlSchemaChoice found under node and parent is not Choice
        public const int SchemaCode128EXmlSchemaChoiceParentChoiceNotFound = 128;
    }

    /*
     * List of X12 Error Codes along with their description. A uniform naming
     * convention would be followed for codes and their description. Note that
     * error codes are not unique, for instance, the same code may be used for
     * Segment or Field level error. However, their meaning would be different
     * and would be in conformance with X12 standard
     */
    public class X12ErrorCode
    {
        #region Standard Segment level error codes
        public const int UnrecognizedSegmentIDCode = 1;
        public const int UnexpectedSegmentCode = 2;
        public const int MandatorySegmentMissingCode = 3;
        public const int LoopOccursOverMaximumTimesCode = 4;
        public const int SegmentExceedsMaximumUseCode = 5;
        public const int SegmentNotInDefinedTSCode = 6;
        public const int SegmentNotInProperSequenceCode = 7;
        public const int SegmentHasDataElementErrorsCode = 8;
        #endregion

        #region Standard Segment level error description
        // TODO: Currently this dictionary contains error description itself
        // however when we support more than 1 language we can change the description
        // to resource file key
        private static Dictionary<int, string> standardSegmentErrorDescriptions = new Dictionary<int, string>()
                {
                     {UnrecognizedSegmentIDCode, "Unexpected segment"}
                    ,{UnexpectedSegmentCode, "Unrecognized segment ID"}
                    ,{MandatorySegmentMissingCode, "Mandatory Segment Missing"}
                    ,{LoopOccursOverMaximumTimesCode, "Loops Occurs Over Maximum Times"}
                    ,{SegmentExceedsMaximumUseCode, "Segment Exceeds Maximum Use Description"}
                    ,{SegmentNotInDefinedTSCode, "Segment Not In Defined Transaction set"}
                    ,{SegmentNotInProperSequenceCode, "Segment Not In Proper Sequence"}
                    ,{SegmentHasDataElementErrorsCode, "Segment Has Data Element Errors"}
                };

        //public const string UnexpectedSegmentDescription = "X12UnexpectedSegmentDescription"; //"Unexpected segment";
        //public const string UnrecognizedSegmentIDDescription = "X12UnrecognizedSegmentIDDescription"; //"Unrecognized segment ID";
        //public const string MandatorySegmentMissingDescription = "X12MandatorySegmentMissingDescription"; //"Mandatory Segment Missing";
        //public const string LoopOccursOverMaximumTimesDescription = "X12LoopOccursOverMaximumTimesDescription"; //"Loops Occurs Over Maximum Times";
        //public const string SegmentExceedsMaximumUseDescription = "X12SegmentExceedsMaximumUseDescription"; //"Segment Exceeds Maximum Use Description";
        //public const string SegmentNotInDefinedTSDescription = "X12SegmentNotInDefinedTSDescription"; //"Segment Not In Defined Transaction set";
        //public const string SegmentNotInProperSequenceDescription = "X12SegmentNotInProperSequenceDescription"; //"Segment Not In Proper Sequence";
        //public const string SegmentHasDataElementErrorsDescription = "X12SegmentHasDataElementErrorsDescription"; //"Segment Has Data Element Errors";

        public static string GetStandardSegmentErrorDescription(int standardSegmentErrorCode)
        {
            string result = string.Empty;

            if (standardSegmentErrorDescriptions.TryGetValue(standardSegmentErrorCode, out result) == false)
            {
                result = string.Format("Standard segment error#{0}", standardSegmentErrorCode);
            }

            return result;
        }
        #endregion

        #region Standard TS Response Trailer Error codes
        public const int TsNotSupportedCode = 1;
        public const int TsTrailerMissingCode = 2;
        public const int TsControlNumberMismatchCode = 3;
        public const int TsIncludedSegCountMismatchCode = 4;
        public const int TsOneOrMoreSegmentsInErrorCode = 5;
        public const int TsMissingOrInvalidTsIdentiferCode = 6;
        public const int TsMissingOrInvalidTsControlNumberCode = 7;
        //8..22 & 24..27 not applicable
        public const int TsControlNumberNotUniqueCode = 23;
        #endregion

        #region Standard TS Response Trailer Error Descriptions
        private static Dictionary<int, string> standardTSErrorDescriptions = new Dictionary<int, string>()
                {
                     {TsNotSupportedCode, "Transaction Set Not Supported"}
                    ,{TsTrailerMissingCode, "Transaction Set Trailer Missing"}
                    ,{TsControlNumberMismatchCode, "Transaction Set Control Number Mismatch"}
                    ,{TsIncludedSegCountMismatchCode, "Number of included segments mismatch"}
                    ,{TsOneOrMoreSegmentsInErrorCode, "One or more segments in error"}
                    ,{TsMissingOrInvalidTsIdentiferCode, "Missing or invalid Transaction set identifier"}
                    ,{TsMissingOrInvalidTsControlNumberCode, "Missing or invalid transaction set control number"}
                    ,{TsControlNumberNotUniqueCode, "Transaction Set duplicate control number found"}
                };

        //public const string TsNotSupportedDescription = "X12TsNotSupportedDescription"; //"Transaction Set Not Supported";
        //public const string TsTrailerMissingDescription = "X12TsTrailerMissingDescription"; //"Transaction Set Trailer Missing";
        //public const string TsControlNumberMismatchDescription = "X12TsControlNumberMismatchDescription"; //"Transaction Set Control Number Mismatch";
        //public const string TsIncludedSegCountMismatchDescription = "X12TsIncludedSegCountMismatchDescription"; //"Number of included segments mismatch";
        //public const string TsOneOrMoreSegmentsInErrorDescription = "X12TsOneOrMoreSegmentsInErrorDescription"; //"One or more segments in error";
        //public const string TsMissingOrInvalidTsIdentiferDescription = "X12TsMissingOrInvalidTsIdentiferDescription"; //"Missing or invalid Transaction set identifier";

        //public const string TsMissingOrInvalidTsControlNumberDescription = "X12TsMissingOrInvalidTsControlNumberDescription"; //"Missing or invalid transaction set control number";
        //public const string TsDuplicateNumberFoundDescription = "X12TsDuplicateNumberFoundDescription"; //"Transaction Set duplicate control number found";

        public static string GetStandardTSErrorDescription(int standardTSErrorCode)
        {
            string result = string.Empty;

            if (standardTSErrorDescriptions.TryGetValue(standardTSErrorCode, out result) == false)
            {
                result = string.Format("Standard TS error#{0}", standardTSErrorCode);
            }

            return result;
        }
        #endregion

        #region Data element level error codes
        public const int DeMandatoryDataElementMissingCode = 1;
        public const int DeConditionalRequiredDataElementMissingCode = 2;
        public const int DeTooManyDataElementsCode = 3;
        public const int DeDataElementTooShortCode = 4;
        public const int DeDataElementTooLongCode = 5;
        public const int DeInvalidCharacterInDataElementCode = 6;
        public const int DeInvalidCodeValueCode = 7;
        public const int DeInvalidDateCode = 8;
        public const int DeInvalidTimeCode = 9;
        public const int DeExclusionConditionViolatedCode = 10;
        // Added for X12 Contingency handling
        public const int DeMandatoryIdValueMissingCode = 11;
        public const int DeMandatoryIdValueOrAlternativeValueMissingCode = 12;
        public const int DeCrossSegmentIdValueOccurancesDoesNotMatch = 13;
        #endregion

        #region Data element level error code description
        private static Dictionary<int, string> dataElementErrorDescriptions = new Dictionary<int, string>()
                {
                     {DeMandatoryDataElementMissingCode, "Mandatory data element missing"}
                    ,{DeConditionalRequiredDataElementMissingCode, "Conditional required data element missing"}
                    ,{DeTooManyDataElementsCode, "Too many data elements"}
                    ,{DeDataElementTooShortCode, "Data element too short"}
                    ,{DeDataElementTooLongCode, "Data element too long"}
                    ,{DeInvalidCharacterInDataElementCode, "Invalid character in data element"}
                    ,{DeInvalidCodeValueCode, "Invalid code value"}
                    ,{DeInvalidDateCode, "Invalid Date"}
                    ,{DeInvalidTimeCode, "Invalid Time"}
                    ,{DeExclusionConditionViolatedCode, "Exclusion Condition Violated"}
                    ,{DeMandatoryIdValueMissingCode, "Segment occurance with mandatory id value missing"}
                    ,{DeMandatoryIdValueOrAlternativeValueMissingCode, "Segment occurance with mandatory id value (or alternate value) missing"}
                    ,{DeCrossSegmentIdValueOccurancesDoesNotMatch, "Cross segment value occurances does not match (both should present or absent)"}
                };

        //public const string DeMandatoryDataElementMissingDescription = "X12DeMandatoryDataElementMissingDescription"; //"Mandatory data element missing";
        //public const string DeConditionalRequiredDataElementMissingDescription = "X12DeConditionalRequiredDataElementMissingDescription"; //"Conditional required data element missing";
        //public const string DeTooManyDataElementsDescription = "X12DeTooManyDataElementsDescription"; //"Too many data elements";
        //public const string DeDataElementTooShortDescription = "X12DeDataElementTooShortDescription"; //"Data element too short";
        //public const string DeDataElementTooLongDescription = "X12DeDataElementTooLongDescription"; //"Data element too long";
        //public const string DeInvalidCharacterInDataElementDescription = "X12DeInvalidCharacterInDataElementDescription"; //"Invalid character in data element";
        //public const string DeInvalidCodeValueDescription = "X12DeInvalidCodeValueDescription"; //"Invalid code value";
        //public const string DeInvalidDateDescription = "X12DeInvalidDateDescription"; //"Invalid Date";
        //public const string DeInvalidTimeDescription = "X12DeInvalidTimeDescription"; //"Invalid Time";
        //public const string DeExclusionConditionViolatedDescription = "X12DeExclusionConditionViolatedDescription"; //"Exclusion Condition Violated";

        public static string GetDataElementErrorDescription(int dataElementErrorCode)
        {
            string result = string.Empty;

            if (dataElementErrorDescriptions.TryGetValue(dataElementErrorCode, out result) == false)
            {
                result = string.Format("Data element error#{0}", dataElementErrorCode);
            }

            return result;
        }
        #endregion

        #region TA1 Error codes
        public const int Ta1NoErrorCode = 0;
        public const int Ta1InterchangeControlNumberMismatchCode = 1; //Nse
        public const int Ta1InvalidControlStandardIdentifierCode = 2;//16; //Isa11 error, # 3337 //TODO: This error code wa 16, however 16 already exist below
        public const int Ta1InvalidVersionIdentifierCode = 3; //Isa12 error
        public const int Ta1InvalidSegmentTerminatorCode = 4; //currently not generated
        public const int Ta1InvalidSenderIdQualifierCode = 5; //Isa5 error
        public const int Ta1InvalidSenderIdCode = 6; //Isa6 error
        public const int Ta1InvalidReceiverIdQualifierCode = 7; //Isa7
        public const int Ta1InvalidReceiverIdCode = 8; //Isa8
        public const int Ta1UnknownReceiverIdCode = 9; //not generated
        public const int Ta1InvalidAuthorizationQualifierCode = 10; //Isa1
        public const int Ta1InvalidAuthorizationValueCode = 11; //Isa2
        public const int Ta1InvalidSecurityQualifierCode = 12; //Isa3
        public const int Ta1InvalidSecurityValueCode = 13; //Isa4
        public const int Ta1InvalidDateCode = 14; //Isa9
        public const int Ta1InvalidTimeCode = 15; //Isa10
        public const int Ta1InvalidStandardsValueCode = 16; //not generated
        public const int Ta1InvalidVersionIdCode = 17;
        public const int Ta1InvalidControlNumberValueCode = 18; //Isa13
        public const int Ta1InvalidAckRequestedValueCode = 19; //Isa14
        public const int Ta1InvalidTestIndicatorValueCode = 20; //Isa15
        public const int Ta1InvalidNumberOfIncludedGroupsCode = 21;
        public const int Ta1InvalidControlStructureCode = 22;
        public const int Ta1ImproperEofCode = 23;
        public const int Ta1InvalidInterchangeContentCode = 24;
        public const int Ta1DuplicateControlNumberCode = 25;
        public const int Ta1InvalidDataElementSeparatorCode = 26; //can be used when separators are not unique
        public const int Ta1InvalidComponentElementSeparatorCode = 27;
        //public const int Code = 28;
        //public const int Code = 29;
        //public const int Code = 30;
        //public const int Code = 31;
        #endregion

        #region TA1 error description

        private static Dictionary<int, string> ta1ErrorDescriptions = new Dictionary<int, string>()
                {
                     {Ta1NoErrorCode, "No error"}
                    ,{Ta1InterchangeControlNumberMismatchCode, "Control Number in ISA and IEA do not match"} //Nse
                    ,{Ta1InvalidControlStandardIdentifierCode, "Invalid Control Standard Identifier"} //Isa11 error
                    ,{Ta1InvalidVersionIdentifierCode, "Invalid Version Identifier"} //Isa12 error
                    ,{Ta1InvalidSegmentTerminatorCode, "Invalid Segment Terminator"} //currently not generated
                    ,{Ta1InvalidSenderIdQualifierCode, "Invalid SenderId Qualifier"} //Isa5 error
                    ,{Ta1InvalidSenderIdCode, "Invalid SenderId"} //Isa6 error
                    ,{Ta1InvalidReceiverIdQualifierCode, "Invalid ReceiverId Qualifier"} //Isa7
                    ,{Ta1InvalidReceiverIdCode, "Invalid ReceiverId"} //Isa8
                    ,{Ta1UnknownReceiverIdCode, "Unknown ReceiverIdD"} //not generated
                    ,{Ta1InvalidAuthorizationQualifierCode, "Invalid Authorization Qualifier"} //Isa1
                    ,{Ta1InvalidAuthorizationValueCode, "Invalid Authorization Value"} //Isa2
                    ,{Ta1InvalidSecurityQualifierCode, "Invalid Security Qualifier"} //Isa3
                    ,{Ta1InvalidSecurityValueCode, "Invalid Security Value"} //Isa4
                    ,{Ta1InvalidDateCode, "Invalid Date"} //Isa9
                    ,{Ta1InvalidTimeCode, "Invalid Time"} //Isa10
                    ,{Ta1InvalidStandardsValueCode, "Invalid Standards Value"} //not generated
                    ,{Ta1InvalidVersionIdCode, "Invalid VersionId"}
                    ,{Ta1InvalidControlNumberValueCode, "Invalid Control Number Value"} //Isa13
                    ,{Ta1InvalidAckRequestedValueCode, "Invalid AckRequested Value"} //Isa14
                    ,{Ta1InvalidTestIndicatorValueCode, "Invalid Test Indicator Value"} //Isa15
                    ,{Ta1InvalidNumberOfIncludedGroupsCode, "Invalid Number Of Included Groups"}
                    ,{Ta1InvalidControlStructureCode, "Invalid Control Structure"}
                    ,{Ta1ImproperEofCode, "Improper End of File"}
                    ,{Ta1InvalidInterchangeContentCode, "Invalid Interchange Content"}
                    ,{Ta1DuplicateControlNumberCode, "Duplicate Control Number"}
                    ,{Ta1InvalidDataElementSeparatorCode, "Invalid Data Element Separator"} //can be used when separators are not unique
                    ,{Ta1InvalidComponentElementSeparatorCode, "Invalid Component Element Separator"} //"Invalid Component Element Separator";
                };

        //public const string Ta1NoErrorDescription = "X12Ta1NoErrorDescription"; //"No error";
        //public const string Ta1InterchangeControlNumberMismatchDescription = "X12Ta1InterchangeControlNumberMismatchDescription"; //"Control Number in ISA and IEA do not match"; //Nse
        //public const string Ta1InvalidControlStandardIdentifierDescription = "X12Ta1InvalidControlStandardIdentifierDescription"; //"Invalid Control Standard Identifier"; //Isa11 error
        //public const string Ta1InvalidVersionIdentifierDescription = "X12Ta1InvalidVersionIdentifierDescription"; //"Invalid Version Identifier"; //Isa12 error
        //public const string Ta1InvalidSegmentTerminatorDescription = "X12Ta1InvalidSegmentTerminatorDescription"; //"Invalid Segment Terminator"; //currently not generated
        //public const string Ta1InvalidSenderIdQualifierDescription = "X12Ta1InvalidSenderIdQualifierDescription"; //"Invalid SenderId Qualifier"; //Isa5 error
        //public const string Ta1InvalidSenderIdDescription = "X12Ta1InvalidSenderIdDescription"; //"Invalid SenderId"; //Isa6 error
        //public const string Ta1InvalidReceiverIdQualifierDescription = "X12Ta1InvalidReceiverIdQualifierDescription"; //"Invalid ReceiverId Qualifier"; //Isa7
        //public const string Ta1InvalidReceiverIdDescription = "X12Ta1InvalidReceiverIdDescription"; //"Invalid ReceiverId"; //Isa8
        //public const string Ta1UnknownReceiverIdDescription = "X12Ta1UnknownReceiverIdDescription"; //"Unknown ReceiverIdD"; //not generated
        //public const string Ta1InvalidAuthorizationQualifierDescription = "X12Ta1InvalidAuthorizationQualifierDescription"; //"Invalid Authorization Qualifier"; //Isa1
        //public const string Ta1InvalidAuthorizationValueDescription = "X12Ta1InvalidAuthorizationValueDescription"; //"Invalid Authorization Value"; //Isa2
        //public const string Ta1InvalidSecurityQualifierDescription = "X12Ta1InvalidSecurityQualifierDescription"; //"Invalid Security Qualifier"; //Isa3
        //public const string Ta1InvalidSecurityValueDescription = "X12Ta1InvalidSecurityValueDescription"; //"Invalid Security Value"; //Isa4
        //public const string Ta1InvalidDateDescription = "X12Ta1InvalidDateDescription"; //"Invalid Date"; //Isa9
        //public const string Ta1InvalidTimeDescription = "X12Ta1InvalidTimeDescription"; //"Invalid Time"; //Isa10
        //public const string Ta1InvalidStandardsValueDescription = "X12Ta1InvalidStandardsValueDescription"; //"Invalid Standards Value"; //not generated
        //public const string Ta1InvalidVersionIdDescription = "X12Ta1InvalidVersionIdDescription"; //"Invalid VersionId";
        //public const string Ta1InvalidControlNumberValueDescription = "X12Ta1InvalidControlNumberValueDescription"; //"Invalid Control Number Value"; //Isa13
        //public const string Ta1InvalidAckRequestedValueDescription = "X12Ta1InvalidAckRequestedValueDescription"; //"Invalid AckRequested Value"; //Isa14
        //public const string Ta1InvalidTestIndicatorValueDescription = "X12Ta1InvalidTestIndicatorValueDescription"; //"Invalid Test Indicator Value"; //Isa15
        //public const string Ta1InvalidNumberOfIncludedGroupsDescription = "X12Ta1InvalidNumberOfIncludedGroupsDescription"; //"Invalid Number Of Included Groups";
        //public const string Ta1InvalidControlStructureDescription = "X12Ta1InvalidControlStructureDescription"; //"Invalid Control Structure";
        //public const string Ta1ImproperEofDescription = "X12Ta1ImproperEofDescription"; //"Improper End of File";
        //public const string Ta1InvalidInterchangeContentDescription = "X12Ta1InvalidInterchangeContentDescription"; //"Invalid Interchange Content";
        //public const string Ta1DuplicateControlNumberDescription = "X12Ta1DuplicateControlNumberDescription"; //"Duplicate Control Number";
        //public const string Ta1InvalidDataElementSeparatorDescription = "X12Ta1InvalidDataElementSeparatorDescription"; //"Invalid Data Element Separator"; //can be used when separators are not unique
        //public const string Ta1InvalidComponentElementSeparatorDescription = "X12Ta1InvalidComponentElementSeparatorDescription"; //"Invalid Component Element Separator";

        public static string GetTa1ErrorDescription(int ta1ErrorCode)
        {
            string result = string.Empty;

            if (ta1ErrorDescriptions.TryGetValue(ta1ErrorCode, out result) == false)
            {
                result = string.Format("TA1 error#{0}", ta1ErrorCode);
            }

            return result;
        }
        #endregion

        #region FA Error codes
        //3, 10-26 not supported
        public const int FaGroupNotSupportedCode = 1; //GS01
        public const int FaGroupVersionNotSupportedCode = 2; //GS08
        public const int FaControlNumberMismatchCode = 4;
        public const int FaNumberOfTsMismatchCode = 5;
        public const int FaInvalidControlNumberCode = 6; //GS06
        public const int FaGsSeqNoIsDuplicate = 100;
        #endregion

        #region FA Error description
        private static Dictionary<int, string> faErrorDescriptions = new Dictionary<int, string>()
                {
                     {FaGroupNotSupportedCode, "Functional Group Not Supported"} //GS01
                    ,{FaGroupVersionNotSupportedCode, "Functional Group Version Not Supported"} //GS08
                    ,{FaControlNumberMismatchCode, "Group control number in header and tailer do not match"}
                    ,{FaNumberOfTsMismatchCode, "Number of included transaction sets do not match"}
                    ,{FaInvalidControlNumberCode, "Group control number violates syntax"}
                    ,{FaGsSeqNoIsDuplicate, "Duplicate group control number found"}
                };

        //3, 10-26 not supported
        //public const string FaGroupNotSupportedDescription = "X12FaGroupNotSupportedDescription"; //"Functional Group Not Supported"; //GS01
        //public const string FaGroupVersionNotSupportedDescription = "X12FaGroupVersionNotSupportedDescription"; //"Functional Group Version Not Supported"; //GS08
        //public const string FaControlNumberMismatchDescription = "X12FaControlNumberMismatchDescription"; //"Group control number in header and tailer do not match";
        //public const string FaNumberOfTsMismatchDescription = "X12FaNumberOfTsMismatchDescription"; //"Number of included transaction sets do not match";
        //public const string FaInvalidControlNumberDescription = "X12FaInvalidControlNumberDescription"; //"Group control number violates syntax";
        //public const string FaGsSeqNoIsDuplicateDescription = "X12FaGsSeqNoIsDuplicateDescription"; //"Duplicate group control number found";

        public static string GetFaErrorDescription(int faErrorCode)
        {
            string result = string.Empty;

            if (faErrorDescriptions.TryGetValue(faErrorCode, out result) == false)
            {
                result = string.Format("FA error#{0}", faErrorCode);
            }

            return result;
        }
        #endregion

        #region Non-standard X12 codes

        public const int ConfigSettingsValidationErrorCode = 518;
        public const int NseSchemaNotFoundCode = 502;
        public const int SeFsNotFoundAfterTagIdCode = 503;
        public const int FeTooFewDataElementsFoundCode = 504;
        public const int NseStructurallyInvalidElementCode = 505;
        public const int SeNodeCannotRepeatCode = 3;
        public const int FeCannotEndWithTagCode = 507;
        public const int FeRepeatsMoreThanRequiredCode = 33; // TODO: This code was 3, but 3 already exist above
        public const int FeRepeatsLessThanRequiredCode = 2;
        public const int FeCrossFieldViolationCode = 6;
        public const int SeSegmentHasTrailingDelimiterCode = 66; // TODO: This code was 6, but 6 already exist above
        public const int SeNoMatchingXmlNodeFoundCode = 8;
        public const int SeSegmentRepeatsLessTimesCode = 333; // TODO: This code was 3, but 3 already exist above
        public const int SeLoopRepeatsLessTimesCode = 7;
        public const int NseXmlNotAtCorrectPositionCode = 515;
        public const int NseXmlValidationErrorCatchAll = 516;
        public const int NseTransactionSetNotAllowed = 517;
        public const int NseAgreementResolutionFailed = 519;

        #endregion

        #region Non-standard X12 error description
        private static Dictionary<int, string> nonStandardX12ErrorDescriptions = new Dictionary<int, string>()
                {
                     {ConfigSettingsValidationErrorCode, "Document spec type {0} not found"}
                    ,{NseSchemaNotFoundCode, "Field separator not found after segment tag id"}
                    ,{SeFsNotFoundAfterTagIdCode, "Too few data elements found"}
                    ,{FeTooFewDataElementsFoundCode, "The element '{0}' has an invalid structure"}
                    ,{NseStructurallyInvalidElementCode, "Node cannot repeat"}
                    ,{SeNodeCannotRepeatCode, "The data for the parent entity cannot end with child tag <{0}>"}
                    ,{FeCannotEndWithTagCode, "Repeats more than required"}
                    ,{FeRepeatsMoreThanRequiredCode, "Repeats less than required"}
                    ,{FeRepeatsLessThanRequiredCode, "Cross field validation rule violated"}
                    ,{FeCrossFieldViolationCode, "Segment has trailing delimiter(s) at component or sub-component level"}
                    ,{SeSegmentHasTrailingDelimiterCode, "No matching child node found"}
                    ,{SeNoMatchingXmlNodeFoundCode, "Segment repeats less than the minimum allowed number of times"}
                    ,{SeSegmentRepeatsLessTimesCode, "Loop repeats less than the minimum allowed number of times"}
                    ,{SeLoopRepeatsLessTimesCode, "During serialization root node is not placed at start element"}
                    ,{NseXmlNotAtCorrectPositionCode, "Nse xml not at correct position"}
                    ,{NseXmlValidationErrorCatchAll, "Nse xml validation error catch all"}
                    ,{NseTransactionSetNotAllowed, "Nse transaction set not allowed"}
                    ,{NseAgreementResolutionFailed, "Nse agreement resolution failed"}
                };

        //public const string NseSchemaNotFoundDescription = "X12NseSchemaNotFoundDescription"; //"Document spec type {0} not found";
        //public const string SeFsNotFoundAfterTagIdDescription = "X12SeFsNotFoundAfterTagIdDescription"; //"Field separator not found after segment tag id";
        //public const string FeTooFewDataElementsFoundDescription = "X12FeTooFewDataElementsFoundDescription"; //"Too few data elements found";
        //public const string NseStructurallyInvalidElementDescription = "X12NseStructurallyInvalidElementDescription"; //"The element '{0}' has an invalid structure";
        //public const string SeNodeCannotRepeatDescription = "X12SeNodeCannotRepeatDescription"; //"Node cannot repeat";
        //public const string FeCannotEndWithTagDescription = "X12FeCannotEndWithTagDescription"; //"The data for the parent entity cannot end with child tag <{0}>";
        //public const string FeRepeatsMoreThanRequiredDescription = "X12FeRepeatsMoreThanRequiredDescription"; //"Repeats more than required";
        //public const string FeRepeatsLessThanRequiredDescription = "X12FeRepeatsLessThanRequiredDescription"; //"Repeats less than required";
        //public const string FeCrossFieldViolationDescription = "X12FeCrossFieldViolationDescription"; //"Cross field validation rule violated";
        //public const string SeSegmentHasTrailingDelimiterDescription = "X12SeSegmentHasTrailingDelimiterDescription"; //"Segment has trailing delimiter(s) at component or sub-component level";
        //public const string SeNoMatchingXmlNodeFoundDescription = "X12SeNoMatchingXmlNodeFoundDescription"; //"No matching child node found";
        //public const string SeSegmentRepeatsLessTimesDescription = "X12SeSegmentRepeatsLessTimesDescription"; //"Segment repeats less than the minimum allowed number of times";
        //public const string SeLoopRepeatsLessTimesDescription = "X12SeLoopRepeatsLessTimesDescription"; //"Loop repeats less than the minimum allowed number of times";
        //public const string NseXmlNotAtCorrectPositionDescription = "X12NseXmlNotAtCorrectPositionDescription"; //"During serialization root node is not placed at start element";
        //public const string X12DataElementLeadingOrTrailingSpaceFoundDescription = "DataElementLeadingOrTrailingSpaceFound";
        //public const string NseTransactionSetNotAllowedDescription = "TransactionSetNotAllowedDescription";

        public static string GetNonStandardX12ErrorDescription(int nonStandardX12ErrorCode)
        {
            string result = string.Empty;

            if (nonStandardX12ErrorDescriptions.TryGetValue(nonStandardX12ErrorCode, out result) == false)
            {
                result = string.Format("Non X12 error#{0}", nonStandardX12ErrorCode);
            }

            return result;
        }
        #endregion

        ////=========================================================================================
        ///// <summary>
        ///// Helper method to return localized error information for functional ack error code integer values.
        ///// </summary>
        ///// <param name="ta1ErrorCode"></param>
        ///// <returns></returns>
        ////=========================================================================================

        //public static string GetFaErrorDescription(int faErrorCode)
        //{
        //    string result = string.Empty;

        //    switch (faErrorCode)
        //    {
        //        case FaGroupNotSupportedCode:
        //            result = FaGroupNotSupportedDescription;
        //            break;

        //        case FaGroupVersionNotSupportedCode:
        //            result = FaGroupVersionNotSupportedDescription;
        //            break;

        //        case FaControlNumberMismatchCode:
        //            result = FaControlNumberMismatchDescription;
        //            break;

        //        case FaNumberOfTsMismatchCode:
        //            result = FaNumberOfTsMismatchDescription;
        //            break;


        //        case FaInvalidControlNumberCode:
        //            result = FaInvalidControlNumberDescription;
        //            break;

        //        case FaGsSeqNoIsDuplicate:
        //            result = FaGsSeqNoIsDuplicateDescription;
        //            break;

        //        default:
        //            break;
        //    }

        //    return result;
        //}

        ////=========================================================================================
        ///// <summary>
        ///// Helper method to return localized error information for TA1 error code integer values.
        ///// </summary>
        ///// <param name="ta1ErrorCode"></param>
        ///// <returns></returns>
        ////=========================================================================================

        //public static string GetTa1ErrorDescription(int ta1ErrorCode)
        //{
        //    string result = string.Empty;

        //    switch (ta1ErrorCode)
        //    {
        //        case Ta1NoErrorCode:
        //            result = Ta1NoErrorDescription;
        //            break;

        //        case Ta1InterchangeControlNumberMismatchCode:
        //            result = Ta1InterchangeControlNumberMismatchDescription;
        //            break;

        //        //case Ta1InvalidControlStandardIdentifierCode:
        //        //  result = Ta1InvalidControlStandardIdentifierDescription);
        //        //break;

        //        case Ta1InvalidVersionIdentifierCode:
        //            result = Ta1InvalidVersionIdentifierDescription;
        //            break;

        //        case Ta1InvalidSegmentTerminatorCode:
        //            result = Ta1InvalidSegmentTerminatorDescription;
        //            break;

        //        case Ta1InvalidSenderIdQualifierCode:
        //            result = Ta1InvalidSenderIdQualifierDescription;
        //            break;

        //        case Ta1InvalidSenderIdCode:
        //            result = Ta1InvalidSenderIdDescription;
        //            break;

        //        case Ta1InvalidReceiverIdQualifierCode:
        //            result = Ta1InvalidReceiverIdQualifierDescription;
        //            break;

        //        case Ta1InvalidReceiverIdCode:
        //            result = Ta1InvalidReceiverIdDescription;
        //            break;

        //        case Ta1UnknownReceiverIdCode:
        //            result = Ta1UnknownReceiverIdDescription;
        //            break;

        //        case Ta1InvalidAuthorizationQualifierCode:
        //            result = Ta1InvalidAuthorizationQualifierDescription;
        //            break;

        //        case Ta1InvalidAuthorizationValueCode:
        //            result = Ta1InvalidAuthorizationValueDescription;
        //            break;

        //        case Ta1InvalidSecurityQualifierCode:
        //            result = Ta1InvalidSecurityQualifierDescription;
        //            break;

        //        case Ta1InvalidSecurityValueCode:
        //            result = Ta1InvalidSecurityValueDescription;
        //            break;

        //        case Ta1InvalidDateCode:
        //            result = Ta1InvalidDateDescription;
        //            break;

        //        case Ta1InvalidTimeCode:
        //            result = Ta1InvalidTimeDescription;
        //            break;

        //        case Ta1InvalidStandardsValueCode:
        //            result = Ta1InvalidStandardsValueDescription;
        //            break;

        //        case Ta1InvalidVersionIdCode:
        //            result = Ta1InvalidVersionIdDescription;
        //            break;

        //        case Ta1InvalidControlNumberValueCode:
        //            result = Ta1InvalidControlNumberValueDescription;
        //            break;

        //        case Ta1InvalidAckRequestedValueCode:
        //            result = Ta1InvalidAckRequestedValueDescription;
        //            break;

        //        case Ta1InvalidTestIndicatorValueCode:
        //            result = Ta1InvalidTestIndicatorValueDescription;
        //            break;

        //        case Ta1InvalidNumberOfIncludedGroupsCode:
        //            result = Ta1InvalidNumberOfIncludedGroupsDescription;
        //            break;

        //        case Ta1InvalidControlStructureCode:
        //            result = Ta1InvalidControlStructureDescription;
        //            break;

        //        case Ta1ImproperEofCode:
        //            result = Ta1ImproperEofDescription;
        //            break;

        //        case Ta1InvalidInterchangeContentCode:
        //            result = Ta1InvalidInterchangeContentDescription;
        //            break;

        //        case Ta1DuplicateControlNumberCode:
        //            result = Ta1DuplicateControlNumberDescription;
        //            break;

        //        case Ta1InvalidDataElementSeparatorCode:
        //            result = Ta1InvalidDataElementSeparatorDescription;
        //            break;

        //        case Ta1InvalidComponentElementSeparatorCode:
        //            result = Ta1InvalidComponentElementSeparatorDescription;
        //            break;

        //        default:
        //            break;
        //    }

        //    return result;
        //}
    }
}
