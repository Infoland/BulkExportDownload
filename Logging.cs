using System;
using System.IO;
using System.Net.Mail;

namespace BulkExportDownload
{
    class Logging
    {

        static public void SendEmail(string body)
        {
            string from = ApplicationSettings.FromEMailAddress;
            string to = ApplicationSettings.ToEMailAddress;

            WriteToLog($"Sending e-mail message to '{to}'.");
            WriteToLog($"Adding the following message to e-mail body: \n{body}\n\n");


            MailMessage mail = new MailMessage(from, to);
            SmtpClient client = new SmtpClient();
            mail.IsBodyHtml = false;
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            if (ApplicationSettings.UseCredentialsForMail)
                client.Credentials = new System.Net.NetworkCredential(ApplicationSettings.MailUser, ApplicationSettings.MailPassword);
            else
                client.UseDefaultCredentials = false;

            client.Host = ApplicationSettings.MailServer;
            client.EnableSsl = ApplicationSettings.EnableSSLMail;
            mail.Subject = ApplicationSettings.EmailSubject;
            mail.Body = body;

            // attach logfile, if it exists
            string logFile = $"{AppDomain.CurrentDomain.BaseDirectory}\\Log\\Export_{DateTime.Now.ToString("yyyy-MM-dd")}.log";
            if (File.Exists(logFile))
            {
                mail.Attachments.Add(new Attachment(logFile));
            }

            try
            {
                client.Send(mail);
            }
            catch (Exception e)
            {
                // Dispose of the mail, otherwise it will classify the logfile as "Locked / In use".
                string attachmentName = "";
                if (mail.Attachments.Count > 0)
                {
                    attachmentName = mail.Attachments[0].Name;
                }

                mail.Dispose();

                WriteToLog($"Error occured when sending mail to {ApplicationSettings.MailServer}.\n {e.Message} \n {e.StackTrace}");
                WriteToLog($"From: {ApplicationSettings.FromEMailAddress}");
                WriteToLog($"To: {ApplicationSettings.ToEMailAddress}");
                if (attachmentName != "")
                    WriteToLog($"Attachment(s): {attachmentName}");
                WriteToLog($"Subject: \"{ApplicationSettings.EmailSubject}\"");
                WriteToLog($"Contents of the mail: \n {body}");
            }

        }

        static public void WriteToLog(string message)
        {
            // Write the text to the console.
            Console.WriteLine(message);

            string logfolder = $"{AppDomain.CurrentDomain.BaseDirectory}\\Log\\";
            if (!Directory.Exists(logfolder))
            {
                Directory.CreateDirectory(logfolder);
            }

            // Write the text to a log file.
            string filename = $"{logfolder}Export_{DateTime.Now.ToString("yyyy-MM-dd")}.log";

            using (FileStream fs = new FileStream(filename, FileMode.Append))
            {
                using (StreamWriter log = new StreamWriter(fs))
                {
                    log.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff")}] - {message}");
                }
            }
        }


    }
}
