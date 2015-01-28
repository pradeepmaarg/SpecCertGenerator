using System;

namespace Maarg.Contracts
{
    [Serializable]
    public class MessageStatus
    {
        private string errorDescription;
        private int numberOfRetryAttempts;
        private MessageProcessingResult processingResult;

        /// <summary>
        /// Gets or sets the message in Error, that would determine whether it is suspended or not.
        /// </summary>
        public string ErrorDescription 
        { 
            get { return this.errorDescription; } 
            set { this.errorDescription = value; } 
        }

        public int NumberOfRetryAttempts
        {
            get { return this.numberOfRetryAttempts; }
            set { this.numberOfRetryAttempts = value; }
        }

        public MessageProcessingResult ProcessingResult 
        { 
            get { return this.processingResult; } 
            set { this.processingResult = value; } 
        }
    }
}
