Explanation of the config file (BulkExportDownload.exe.config):

Url: Contains the full iProva Url of the iProva where the bulk export resides, ie: https://organisationiprovaurl.com

ApiKey: The API key set in the application mnagement of the iProva used to download the bulk export from

Username: login code of the user that has permissions to download the bulk export (user needs "application management - bulk export" permissions in iProva)
Note: it is not (yet) allowed to use 2-factor authentication for this user

AllowDownloadOfExportWithErrors: Determines if the Bulk Export Downloader Tool is allowed to download Bulk Exports which have been classified as "Ready, with Errors".

BulkExportsToDownload: Contains the id of the bulk export (can be found in the URL of the details page of the bulk export in iProva) and the path to save the bulkexport to separated by ",". Furthermore multiple bulk exports can be configured, use ; to separate the sets.
ie: 3, c:\bulkexports\bulkexport3\; 2, \\file\Infoland\Bulkexports\Bulkexport2\

SaveCopyOfZipFile: Determines if the downloaded ZIP file should be removed, after it has finished extracting the files.

CleanUpPreviousExport: Determines if the Previously exported files (Backups) need to be cleaned up before the files of the most recent ZIP file will be extracted.

DebugMode: Can be False or True, when True will add extra debugging information to the notification email message

ToEMailAddress: the email adress of the user to notify of any errors that occurred while running the tool

FromEMailAddress: The from email address of used to send the notification email from

MailServer: The mailserver (DNS) to use to send emails with, for example mymailserver.com


