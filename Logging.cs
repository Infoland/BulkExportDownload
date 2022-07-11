using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;

namespace BulkExportDownload
{
    class Logging
    {

        static string emailBody;

        static public void addMessageToEmailBody(string message)
        {
            writeToLog(string.Format("Adding the following message to e-mail body: \n{0}\n\n", message));
            emailBody += message + "\n\n";
        }

        static public void SendEmail()
        {
            string from = Properties.Settings.Default.FromEMailAddress;
            string to = Properties.Settings.Default.ToEMailAddress;

            writeToLog(string.Format("Sending e-mail message to '{0}'.", to));

            MailMessage mail = new MailMessage(from, to);
            SmtpClient client = new SmtpClient();
            mail.IsBodyHtml = false;
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = Properties.Settings.Default.MailServer;
            mail.Subject = Properties.Settings.Default.EMailSubject;
            mail.Body = emailBody;

            // attach logfile, if it exists
            string logFile = string.Format("{0}\\Log\\Export_{1}.log", AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyy-MM-dd"));
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

                writeToLog(String.Format("Error occured when sending mail to {0}.\n {1} \n {2}", Properties.Settings.Default.MailServer, e.Message, e.StackTrace));
                writeToLog(String.Format("From: {0}", Properties.Settings.Default.FromEMailAddress));
                writeToLog(String.Format("To: {0}", Properties.Settings.Default.ToEMailAddress));
                if (attachmentName != "")
                    writeToLog(String.Format("Attachment(s): {0}", attachmentName));
                writeToLog(String.Format("Subject: \"{0}\"", Properties.Settings.Default.EMailSubject));
                writeToLog(String.Format("Contents of the mail: \n {0}", emailBody));
            }

        }

        static public void writeToLog(string message)
        {
            // Write the text to the console.
            Console.WriteLine(message);

            string logfolder = string.Format("{0}\\Log\\", AppDomain.CurrentDomain.BaseDirectory);
            if (!Directory.Exists(logfolder))
            {
                Directory.CreateDirectory(logfolder);
            }

            // Write the text to a log file.
            string filename = String.Format("{0}Export_{1}.log", logfolder, DateTime.Now.ToString("yyyy-MM-dd"));

            using (FileStream fs = new FileStream(filename, FileMode.Append))
            {
                using (StreamWriter log = new StreamWriter(fs))
                {
                    log.WriteLine(String.Format("[{0}] - {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff"), message));
                }
            }
        }


    }
}
