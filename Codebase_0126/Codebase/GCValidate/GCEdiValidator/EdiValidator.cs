using System;
using System.Collections.Generic;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.AllAboard;
using Maarg.Contracts.GCValidate;
using System.IO;
using Maarg.Fatpipe.FlatFilePlug;
using Maarg.Fatpipe.XmlFilePlug;
using System.Diagnostics;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public static class EdiValidator
    {
        /// <summary> This method will pretty up the EDI data to be displayed in UX and passed on to EDIReader. It will add line breaks after every segment </summary>
        /// <param name="ediData">EDI text data (edi file content)</param>
        /// <returns>the formatted data</returns>
        public static string FormatEDIData(string ediData)
        {
            if (string.IsNullOrEmpty(ediData))
            {
                return ediData;
            }

            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(ediData));

            InterchangeTokenizer tokenizer = new InterchangeTokenizer(stream);
            Delimiters delimiters;
            bool isValidEDIDocument = tokenizer.IsX12Interchange(out delimiters);
            if (!isValidEDIDocument) return ediData;
            
            //check whether CR LF is missing, essentially check whether CR is present
            //If so, LF should also be present
            bool crLFPresent = delimiters.SegmentDelimiter == Delimiters.CarriageReturn || delimiters.SegmentDelimiterSuffix1 == Delimiters.CarriageReturn;
            if (!crLFPresent)
            {
                //append every delimiter.SegmentDelimiter with CR LF
                StringBuilder formattedData = new StringBuilder(ediData.Length+100);
                foreach (char ch in ediData)
                {
                    formattedData.Append(ch);
                    if (ch == delimiters.SegmentDelimiter)
                    {
                        formattedData.Append((char)Delimiters.CarriageReturn);
                        formattedData.Append((char)Delimiters.LineFeed);
                    }
                }

                ediData = formattedData.ToString();
            }

            return ediData;
        }

        /// <summary> Wrapper over EDIReader for GC validation feature (Based on spec cert). </summary>
        /// <param name="ediData">EDI text data (edi file content)</param>
        /// <param name="ediFileName">This is for record keeping only, not used by EDIReader</param>
        /// <param name="certFileFullName">Spec cert file (relative path)</param>
        /// <param name="dalManager">To retrieve schema</param>
        /// <returns></returns>
        public static EDIValidationResult ValidateEdi(string ediData, string ediFileName, string certFileFullName, IDalManager dalManager)
        {
            if (string.IsNullOrWhiteSpace(ediData))
                throw new ArgumentNullException("ediData", "Edi file content cannot be empty");

            if (string.IsNullOrWhiteSpace(certFileFullName))
                throw new ArgumentNullException("certFileFullName", "certFileFullName cannot be empty");

            if (dalManager == null)
                throw new ArgumentNullException("dalManager", "dalManager cannot be null");

            TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();
            // Purposely ignoring Initialize function return type (errors) since I don't expect errors here.
            // Spec cert is uploaded only after validation.
            tradingPartnerSpecCertMetadata.Initialize(certFileFullName, null, DateTime.MinValue);

            EDIValidationResult ediValidationResult = new EDIValidationResult()
                {
                    FileName = ediFileName,
                    SchemaName = tradingPartnerSpecCertMetadata.SchemaFileName,
                    SegmentValidationResults = new List<SegmentValidationResult>(),
                    TransactionNumbers = new List<string>(),
                    DisplayName = tradingPartnerSpecCertMetadata.GetCertFileDisplayName(),
                    Type = tradingPartnerSpecCertMetadata.Type,
                };

            try
            {
                IDocumentPlug documentPlug = SchemaCache.GetDocumentPlug(tradingPartnerSpecCertMetadata, dalManager);

                IFatpipeDocument fatpipeDocument;
                ediValidationResult = ValidateEdi(ediData, ediFileName, certFileFullName, documentPlug, out fatpipeDocument);
            }
            catch(Exception ex)
            {
                ediValidationResult.SegmentValidationResults.Add(
                    new SegmentValidationResult()
                        {
                            Type = ResultType.Error,
                            SequenceNumber = -1,
                            Name = "N/A",
                            Description = "Internal error occurred",//ex.Message,
                            StartIndex = -1,
                            EndIndex = -1,
                        }
                    );
            }

            return ediValidationResult;
        }

        /// <summary>Wrapper over EDIReader for GC validation feature (based on Bts assembly file). </summary>
        /// <param name="ediData">EDI text data (edi file content)</param>
        /// <param name="ediFileName">This is for record keeping only, not used by EDIReader</param>
        /// <param name="certFileFullName">Spec cert file (relative path)</param>
        /// <param name="documentPlug">Document plug</param>
        /// <returns></returns>
        public static EDIValidationResult ValidateEdi(string ediData, string ediFileName, string schemaFileName, string certFileDisplayName, string type, SpecCertFileType fileType, 
            IDocumentPlug documentPlug)
        {
            if (string.IsNullOrWhiteSpace(ediData))
                throw new ArgumentNullException("ediData", "Edi file content cannot be empty");

            if (documentPlug == null)
                throw new ArgumentNullException("documentPlug", "documentPlug cannot be null");

            EDIValidationResult ediValidationResult = new EDIValidationResult()
                {
                    FileName = ediFileName,
                    SchemaName = schemaFileName,
                    SegmentValidationResults = new List<SegmentValidationResult>(),
                    TransactionNumbers = new List<string>(),
                    DisplayName = certFileDisplayName,
                    Type = type,
                };

            try
            {
                IFatpipeDocument fatpipeDocument;
                ediValidationResult = ValidateEdi(ediData, ediFileName, schemaFileName, certFileDisplayName, type, fileType, documentPlug, out fatpipeDocument);
            }
            catch (Exception ex)
            {
                ediValidationResult.SegmentValidationResults.Add(
                    new SegmentValidationResult()
                    {
                        Type = ResultType.Error,
                        SequenceNumber = -1,
                        Name = "N/A",
                        Description = "Internal error occurred",//ex.Message,
                        StartIndex = -1,
                        EndIndex = -1,
                    }
                    );
            }

            return ediValidationResult;
        }

        public static EDIValidationResult ValidateEdi(string ediData, string ediFileName, string certFileFullName,
            IDocumentPlug documentPlug, out IFatpipeDocument fatpipeDocument)
        {
            if (string.IsNullOrWhiteSpace(ediData))
                throw new ArgumentNullException("ediData", "Edi file content cannot be empty");

            if (string.IsNullOrWhiteSpace(certFileFullName))
                throw new ArgumentNullException("certFileFullName", "certFileFullName cannot be empty");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();
            // Purposely ignoring Initialize function return type (errors) since I don't expect errors here.
            // Spec cert is uploaded only after validation.
            tradingPartnerSpecCertMetadata.Initialize(certFileFullName, null, DateTime.MinValue);

            return ValidateEdi(ediData, ediFileName, tradingPartnerSpecCertMetadata.SchemaFileName, tradingPartnerSpecCertMetadata.GetCertFileDisplayName(),
                tradingPartnerSpecCertMetadata.Type, (SpecCertFileType)tradingPartnerSpecCertMetadata.FileType, documentPlug, out fatpipeDocument);
        }

        public static EDIValidationResult ValidateEdi(string ediData, string ediFileName, string schemaFileName, string certFileDisplayName, string type, SpecCertFileType fileType,
            IDocumentPlug documentPlug, out IFatpipeDocument fatpipeDocument)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            EDIValidationResult ediValidationResult = new EDIValidationResult()
                {
                    FileName = ediFileName,
                    SchemaName = schemaFileName,
                    SegmentValidationResults = new List<SegmentValidationResult>(),
                    TransactionNumbers = new List<string>(),
                    DisplayName = certFileDisplayName,
                    Type = type,
                };

            fatpipeDocument = null;

            try
            {
                ediValidationResult.SegmentValidationResults.Clear();
                string endHeader = string.Empty;

                switch (fileType)
                {
                    case SpecCertFileType.X12:
                        EDIReader ediReader = new EDIReader();
                        if (ediReader.Initialize(new MemoryStream(Encoding.UTF8.GetBytes(ediData)), null, documentPlug) == true)
                        {
                            try
                            {
                                IFatpipeDocument currentFatpipeDocument = null;
                                while ((currentFatpipeDocument = ediReader.GetNextTransactionSet()) != null)
                                {
                                    if (string.IsNullOrWhiteSpace(ediValidationResult.BeautifiedOriginalPayload))
                                    {
                                        ediValidationResult.BeautifiedOriginalPayload = currentFatpipeDocument.BeautifiedOriginalPayloadStartHeader;
                                        endHeader = currentFatpipeDocument.BeautifiedOriginalPayloadEndHeader;
                                    }

                                    ediValidationResult.BeautifiedOriginalPayload += currentFatpipeDocument.BeautifiedOriginalPayloadBody;
                                    ediValidationResult.TransactionNumbers.Add(currentFatpipeDocument.TransactionNumber);

                                    ediValidationResult.SegmentValidationResults.AddRange(ediReader.Errors.GetSegmentValidationResults());

                                    fatpipeDocument = currentFatpipeDocument;
                                }

                                ediValidationResult.BeautifiedOriginalPayload += endHeader;
                            }
                            catch (EDIReaderException ediReaderException)
                            {
                                // Add whatever errors we accumulated
                                ediValidationResult.SegmentValidationResults.AddRange(ediReader.Errors.GetSegmentValidationResults());

                                ediValidationResult.SegmentValidationResults.Add(
                                    new SegmentValidationResult()
                                        {
                                            Type = ResultType.Error,
                                            SequenceNumber = -1,
                                            Name = "N/A",
                                            Description = ediReaderException.Message,
                                            StartIndex = -1,
                                            EndIndex = -1,
                                        });
                            }
                        }

                        //ediValidationResult.SegmentValidationResults.AddRange(ediReader.Errors.GetSegmentValidationResults());
                        break;

                    case SpecCertFileType.FlatFile:
                        FlatFileReader flatFileReader = new FlatFileReader();
                        fatpipeDocument = flatFileReader.ReadFile(new MemoryStream(Encoding.UTF8.GetBytes(ediData)), documentPlug);

                        ediValidationResult.BeautifiedOriginalPayload = fatpipeDocument.BeautifiedOriginalPayloadBody;
                        ediValidationResult.SegmentValidationResults.AddRange(flatFileReader.Errors.GetSegmentValidationResults());
                        break;

                    case SpecCertFileType.Xml:
                        XmlFileReader xmlFileReader = new XmlFileReader();
                        fatpipeDocument = xmlFileReader.ReadFile(new MemoryStream(Encoding.UTF8.GetBytes(ediData)), documentPlug);

                        ediValidationResult.BeautifiedOriginalPayload = fatpipeDocument.BeautifiedOriginalPayloadBody;
                        ediValidationResult.SegmentValidationResults.AddRange(xmlFileReader.Errors.GetSegmentValidationResults());
                        break;

                    default:
                        ediValidationResult.SegmentValidationResults.Add(
                            new SegmentValidationResult()
                                {
                                    Type = ResultType.Error,
                                    SequenceNumber = -1,
                                    Name = "N/A",
                                    Description = "Invalid cert file type (only EDI and FaltFile is supported)",//ex.Message,
                                    StartIndex = -1,
                                    EndIndex = -1,
                                });
                        break;
                }
            }
            catch (Exception ex)
            {
                ediValidationResult.SegmentValidationResults.Add(
                    new SegmentValidationResult()
                        {
                            Type = ResultType.Error,
                            SequenceNumber = -1,
                            Name = "N/A",
                            //Description = "Internal error occurred",//ex.Message,
                            Description = "Internal error occurred. " + ex.ToString(),
                            StartIndex = -1,
                            EndIndex = -1,
                        });
            }

            sw.Stop();

            ediValidationResult.ExecutionTime = sw.Elapsed;

            return ediValidationResult;
        }
    }
}
