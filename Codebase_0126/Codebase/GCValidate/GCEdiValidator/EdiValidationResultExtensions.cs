using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Contracts.GCValidate;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public static class EdiValidationResultExtensions
    {
        public static List<SegmentValidationResult> GetSegmentValidationResults(this InterchangeErrors ediErrors)
        {
            List<SegmentValidationResult> segmentValidationResults = new List<SegmentValidationResult>();

            if (ediErrors != null && ediErrors.Count > 0)
            {
                if (ediErrors.IsaIeaErrorList != null && ediErrors.IsaIeaErrorList.Count > 0)
                {
                    segmentValidationResults.AddRange( ediErrors.IsaIeaErrorList.GetSegmentValidationResults());
                }

                if (ediErrors.FunctionalGroupErrors != null && ediErrors.FunctionalGroupErrors.Count > 0)
                {
                    foreach(FunctionalGroupErrors functionalGroupErrors in ediErrors.FunctionalGroupErrors)
                    {
                        if (functionalGroupErrors.GsGeErrorList != null && functionalGroupErrors.GsGeErrorList.Count > 0)
                        {
                            segmentValidationResults.AddRange(functionalGroupErrors.GsGeErrorList.GetSegmentValidationResults());
                        }

                        if (functionalGroupErrors.TransactionSetErrors != null && functionalGroupErrors.TransactionSetErrors.Count > 0)
                        {
                            foreach (TransactionSetErrors transactionSetErrors in functionalGroupErrors.TransactionSetErrors)
                            {
                                if (transactionSetErrors.TsErrorList != null && transactionSetErrors.TsErrorList.Count > 0)
                                {
                                    segmentValidationResults.AddRange(transactionSetErrors.TsErrorList.GetSegmentValidationResults());
                                }
                            }
                        }
                    }
                }
            }

            return segmentValidationResults;
        }

        public static IEnumerable<SegmentValidationResult> GetSegmentValidationResults(this EdiSectionErrors ediSectionErrors)
        {
            List<SegmentValidationResult> segmentValidationResults = new List<SegmentValidationResult>();

            foreach (GenericError genericError in ediSectionErrors.GenericErrorList)
            {
                SegmentValidationResult segmentValidationResult = new SegmentValidationResult();
                segmentValidationResult.Type = genericError.EdiErrorType == EdiErrorType.Error ? ResultType.Error : ResultType.Warning;
                segmentValidationResult.SequenceNumber = genericError.PositionInTS;
                segmentValidationResult.Name = genericError.ApproxSegmentTag;
                segmentValidationResult.Description = genericError.Description;
                segmentValidationResult.StartIndex = genericError.StartIndex;
                segmentValidationResult.EndIndex = genericError.EndIndex;

                segmentValidationResults.Add(segmentValidationResult);
            }

            foreach (SegmentError segmentError in ediSectionErrors.SegmentErrorList)
            {
                SegmentValidationResult segmentValidationResult = new SegmentValidationResult();
                segmentValidationResult.Type = segmentError.EdiErrorType == EdiErrorType.Error ? ResultType.Error : ResultType.Warning;
                segmentValidationResult.SequenceNumber = segmentError.PositionInTS;
                segmentValidationResult.Name = segmentError.SegmentID;
                segmentValidationResult.Description = segmentError.Description;
                segmentValidationResult.StartIndex = segmentError.StartIndex;
                segmentValidationResult.EndIndex = segmentError.EndIndex;

                segmentValidationResults.Add(segmentValidationResult);
            }

            foreach (FieldError fieldError in ediSectionErrors.FieldErrorList)
            {
                SegmentValidationResult segmentValidationResult = new SegmentValidationResult();
                segmentValidationResult.Type = fieldError.EdiErrorType == EdiErrorType.Error ? ResultType.Error : ResultType.Warning;
                segmentValidationResult.SequenceNumber = fieldError.PositionInTS;
                segmentValidationResult.Name = fieldError.FieldId;
                segmentValidationResult.Description = fieldError.Description;
                segmentValidationResult.StartIndex = fieldError.StartIndex;
                segmentValidationResult.EndIndex = fieldError.EndIndex;

                segmentValidationResults.Add(segmentValidationResult);
            }

            if (segmentValidationResults.Count > 1)
            {
                IOrderedEnumerable<SegmentValidationResult> orderedList = segmentValidationResults.OrderBy(error => error.SequenceNumber);
                List<SegmentValidationResult> segmentValidationResults2 = new List<SegmentValidationResult>();
                segmentValidationResults2.AddRange(orderedList);
                return segmentValidationResults2;
            }

            return segmentValidationResults;
        }

    }
}
