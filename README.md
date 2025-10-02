# The tool

This is a commandline tool written in C# that can be used to download one or more bulk exports from Zenya using the API.
This tool will download a bulk export and extract it to a configured folder path. 

There are two way to configure this tool, either by using the appsettings.json or by adding settings to your envirnent variables.
- the appsettings.json contains the settings required to run the tool
- the environment variables can be used to override the settings in the appsettings.json. NOTE: prefix the env variables with BulkExport_ this is to prevent conflicts with already	
  existing variables.

> **_NOTE:_** NB2: Even though the working of this tool is not guaranteed, Infoland does supply new build releases which are stored in [Releases](https://github.com/Infoland/BulkExportDownload/releases).

> **_NOTE:_** NB3: These used to be part of the [rest api examples repo](https://github.com/Infoland/iProva-REST-API-examples)


# Explanation of the config file

- **AppRegClientId**: The client id from the registered app registration. Used to authenticate against the Zenya API.
- **AppRegClientSecret**: The client secret from the registered app registration. Used to authenticate against the Zenya API.

- **AllowDownloadOfExportWithErrors**: Determines if the Bulk Export Downloader Tool is allowed to download Bulk Exports which have been classified as "Ready, with Errors".
- **BulkExportsToDownload**: Contains the id of the bulk export (can be found in the URL of the details page of the bulk export in Zenya) and the path to save the bulkexport to separated by ",". Furthermore multiple bulk exports can be configured, use ; to separate the sets.
  For example: 3, c:\bulkexports\bulkexport3\; 2, \\file\Infoland\Bulkexports\Bulkexport2\
- **SaveCopyOfZipFile**: Determines if the downloaded ZIP file should be removed, after it has finished extracting the files.
- **CleanUpPreviousExport**: Determines if the Previously exported files (Backups) need to be cleaned up before the files of the most recent ZIP file will be extracted.
- **WaitForBulkexportToFinish**: Can be False or True, when True will wait for set time for the bulkexport to finish
- **WaitTimeForExportToBeReadyInHours**: number of hours to wait for the bulkexport to finish if setting "WaitForBulkexportToFinish" is set to True
- **DownloadTries**: Number of tries to download a bulkexport
- **Url**: Contains the full Zenya Url of the Zenya where the bulk export resides, ie: https://organisationiprovaurl.com
- **DebugMode**: Can be False or True, when True will add extra debugging information to the notification email message
- **UseBulkexportNameAsFoldername**: Can be False or True, when set to true the folder where the bulkexport is extracted to will be the name of the bulkexport in Zenya. This can cause issues with the full path name in Windows being to long
- **ExtractInnerZIP**: Can be False or True, when True the tool will check if there is a single zip file in the extracted ZIP from Zenya and extracts that to

- **MailServer**: The mailserver (DNS) to use to send emails with, for example mymailserver.com
- **EMailSubject**: The subject line for notification emails sent by the tool
- **FromEMailAddress**: The from email address of used to send the notification email from
- **ToEMailAddress**: the email adress of the user to notify of any errors that occurred while running the tool
- **MailUser**: The user  for the mail server (when not set the Credentials are not passed)
- **MailPassword**: The password for the mail server  (when not set the Credentials are not passed)
- **EnableSSLMail**: Can be False or True, when True there will be an SSL connection used to connect to the mailserver