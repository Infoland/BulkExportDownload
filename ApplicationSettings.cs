namespace BulkExportDownload
{
    public class ApplicationSettings
    {
        public static string[] SaveLocations
        {
            get { return Properties.Settings.Default.BulkExportsToDownload.Split(';'); }
        }

        public static string ZenyaURL
        {
            get { return Properties.Settings.Default.Url; }
        }
        public static string ApiKey
        {
            get { return Properties.Settings.Default.ApiKey; }
        }
        public static string UserName
        {
            get { return Properties.Settings.Default.Username; }
        }

        public static int DownloadTries
        {
            get { return Properties.Settings.Default.DownloadTries; }
        }
        public static bool UseBulkexportNameAsFoldername
        {
            get { return Properties.Settings.Default.UseBulkexportNameAsFoldername; }
        }
        public static bool WaitForBulkexportToFinish
        {
            get { return Properties.Settings.Default.WaitForBulkexportToFinish; }
        }
        public static int WaitTimeForExportToBeReadyInHours
        {
            get { return Properties.Settings.Default.WaitTimeForExportToBeReadyInHours; }
        }
        public static bool AllowDownloadOfExportWithErrors
        {
            get { return Properties.Settings.Default.AllowDownloadOfExportWithErrors; }
        }
        public static bool CleanUpPreviousExport
        {
            get { return Properties.Settings.Default.CleanUpPreviousExport; }
        }
        public static bool SaveCopyOfZipFile
        {
            get { return Properties.Settings.Default.SaveCopyOfZipFile; }
        }
        public static bool DebugMode
        {
            get { return Properties.Settings.Default.DebugMode; }
        }
        public static string FromEMailAddress
        {
            get { return Properties.Settings.Default.FromEMailAddress; }
        }
        public static string ToEMailAddress
        {
            get { return Properties.Settings.Default.ToEMailAddress; }
        }
        public static string MailServer
        {
            get { return Properties.Settings.Default.MailServer; }
        }
        public static string EmailSubject
        {
            get { return Properties.Settings.Default.EMailSubject; }
        }

        public static string MailUser
        {
            get { return Properties.Settings.Default.MailUser; }
        }

        public static string MailPassword
        {
            get { return Properties.Settings.Default.MailPassword; }
        }

        public static bool UseCredentialsForMail
        {
            get { return !string.IsNullOrEmpty(MailUser) && !string.IsNullOrEmpty(MailPassword); }
        }

        public static bool EnableSSLMail
        {
            get { return Properties.Settings.Default.EnableSSLMail; }
        }
        public static bool ExtractInnerZIP
        {
            get { return Properties.Settings.Default.ExtractInnerZIP; }
        }
    }
}
