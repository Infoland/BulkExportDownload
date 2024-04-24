
# The tool

This is a commandline tool written in C# that can be used to download one or more bulk exports from iProva using the API.
This tool will download a bulk export and extract it to a configured folder path. 

The tool comes with a configuration file App.config that can be used to set up the tool to your own needs. 
The settings in the configuration file are explained in the "Readme explaining App.Config settings.txt" file. 

Note: Currently iProva SaaS supports downloading Bulkexport zip files of max 4 GB size.

NB2: Even though the working of this tool is not guaranteed, Infoland does supply new build releases which are stored in [Releases](https://github.com/Infoland/BulkExportDownload/releases).

NB3: These used to be part of the [rest api examples repo](https://github.com/Infoland/iProva-REST-API-examples)



# Explanation of the config file

- Url: Contains the full iProva Url of the iProva where the bulk export resides, ie: https://organisationiprovaurl.com
- ApiKey: The API key set in the application mnagement of the iProva used to download the bulk export from
- Username: login code of the user that has permissions to download the bulk export (user needs "application management - bulk export" permissions in iProva)
   -Note: it is not (yet) allowed to use 2-factor authentication for this user
- AllowDownloadOfExportWithErrors: Determines if the Bulk Export Downloader Tool is allowed to download Bulk Exports which have been classified as "Ready, with Errors".
- BulkExportsToDownload: Contains the id of the bulk export (can be found in the URL of the details page of the bulk export in iProva) and the path to save the bulkexport to separated by ",". Furthermore multiple bulk exports can be configured, use ; to separate the sets.
- ie: 3, c:\bulkexports\bulkexport3\; 2, \\file\Infoland\Bulkexports\Bulkexport2\
- SaveCopyOfZipFile: Determines if the downloaded ZIP file should be removed, after it has finished extracting the files.
- CleanUpPreviousExport: Determines if the Previously exported files (Backups) need to be cleaned up before the files of the most recent ZIP file will be extracted.
- DebugMode: Can be False or True, when True will add extra debugging information to the notification email message
- ToEMailAddress: the email adress of the user to notify of any errors that occurred while running the tool
- FromEMailAddress: The from email address of used to send the notification email from
- MailServer: The mailserver (DNS) to use to send emails with, for example mymailserver.com
- WaitForBulkexportToFinish: Can be False or True, when True will wait for set time for the bulkexport to finish
- WaitTimeForExportToBeReadyInHours: number of hours to wait for the bulkexport to finish if setting "WaitForBulkexportToFinish" is set to True
- DownloadTries: Number of tries to download a bulkexport
- UseBulkexportNameAsFoldername: Can be False or True, when set to true the folder where the bulkexport is extracted to will be the name of the bulkexport in Zenya. This can cause issues with the full path name in Windows being to long
- MailUser: The user  for the mail server (when not set the Credentials are not passed)
- MailPassword: The password for the mail server  (when not set the Credentials are not passed)
