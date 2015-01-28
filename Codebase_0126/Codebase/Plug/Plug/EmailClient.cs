using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;


namespace Maarg.Fatpipe.Plug.DataModel
{
    public class EmailClient
    {
        private static bool SSL = true;
        private static SmtpClient smtpClient = null;
        private static object lockObject = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="connString"></param>
        /// <param name="container"> container where the partner blobs are stored </param>
        public static void InitSmtpSender()
        {
            if (smtpClient == null)
            {
                lock (lockObject)
                {
                    if (smtpClient == null)
                    {
                        string tenantSmtpRelayAccount = "ediactive@cloudmaarg.com";
                        string tenantSmtpRelayAccountPassword = "Maarg2015";
                        string tenantSmtpRelayHost = "smtp.office365.com";

                        smtpClient = new SmtpClient(tenantSmtpRelayHost);
                        NetworkCredential netCred = new NetworkCredential(tenantSmtpRelayAccount, tenantSmtpRelayAccountPassword);
                        smtpClient.EnableSsl = SSL;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = netCred;
                    }
                }
            }
        }

        public static bool SendMessage(string toAddress, string subject, string emailBody)
        {
            return SendMessage(toAddress, subject, emailBody, null);
        }

        public static bool SendMessage(string toAddress, string subject, string emailBody, Attachment[] attachments)
        {
            bool success = true;

            InitSmtpSender();
            string from = "ediactive@cloudmaarg.com";
            if (string.IsNullOrEmpty(toAddress))
            {
                toAddress = "b2bcrew@cloudmaarg.com";
            }

            MailMessage mailMessage = new MailMessage()
            {
                Subject = subject,
                Body = emailBody,
                From = new MailAddress(from)
            };

            if (attachments != null && attachments.Length > 0)
            {
                foreach (Attachment attachment in attachments)
                {
                    mailMessage.Attachments.Add(attachment);
                }
            }

            mailMessage.IsBodyHtml = true;
            mailMessage.To.Add(toAddress);
            smtpClient.Send(mailMessage);

            return success;
        }
    }
}