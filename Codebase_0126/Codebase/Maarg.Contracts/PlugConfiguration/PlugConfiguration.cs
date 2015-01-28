using System;

namespace Maarg.Contracts
{
    [Serializable]
    public class PlugConfiguration : IIdentifier
    {
        //add more in future
        public const int TransportTypeSmtp = 1;
        public const int TransportTypeFtp = 2;

        public string Identifier { get; set; }

        public string SourceName { get; set; }

        //keeping an int instead of enum so it is easily extensible and serializable
        //for now - EDIActivity 4/20 assume SMTP is the only choice
        //so structure is tuned for SMTP
        public int SourceTransportType { get; set; }
        public string SourceSmtpAddress { get; set; }

        public string TargetName { get; set; }

        //keeping an int instead of enum so it is easily extensible and serializable
        //for now - EDIActivity 4/20 assume SMTP is the only choice
        //so structure is tuned for SMTP
		//neww
        public int TargetTransportType { get; set; }
        public string TargetSmtpAddress { get; set; }
    }


}