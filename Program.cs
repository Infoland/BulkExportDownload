using BulkExportDownload.Dtos;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;


namespace BulkExportDownload
{
    class Program
    {
        static internal string emailBody;
        static internal List<SaveLocation> BulkExportsToDownload = new List<SaveLocation>();
        const string backUpDirPrefix = "back_up_";
        static RestAPI restApi = new RestAPI();

        static void Main(string[] args)
        {
            Logging.WriteToLog("Reading Bulk Export Downloader Tool settings");

            //populate list of SaveLocation with the BulkExportsToDownload setting
            string[] saveLocs = ApplicationSettings.SaveLocations;

            Logging.WriteToLog($"Bulk export(s) to download: {saveLocs.Length}");

            foreach (string saveLoc in saveLocs)
            {
                string[] splitLoc = saveLoc.Split(',');
                string id = splitLoc[0].Trim();
                string savePath = splitLoc[1].Trim();
                SaveLocation saveLocation = new SaveLocation(id, savePath);
                BulkExportsToDownload.Add(saveLocation);
            }

            Logging.WriteToLog($"Setting API URL to: {ApplicationSettings.ZenyaURL}");

            emailBody = $"At {DateTime.Now.ToShortDateString()}, the Bulk Export Downloader has verified the state of the given Bulk Export(s) for \"{ApplicationSettings.ZenyaURL}\" and the following steps have been executed:\n";

            foreach (SaveLocation saveLoc in BulkExportsToDownload)
            {
                string bulkExportId = saveLoc.id;
                string savePath = saveLoc.location;
                string bulkExportPath;
                string zipPath;

                var bulkExport = ReadBulkExportFromApi(bulkExportId);

                if (ApplicationSettings.UseBulkexportNameAsFoldername)
                {
                    bulkExportPath = saveLoc.location + "\\" + bulkExport.name;
                    zipPath = saveLoc.location + "\\" + bulkExport.name + ".zip";
                }
                else
                {
                    bulkExportPath = saveLoc.location + "\\" + bulkExportId;
                    zipPath = saveLoc.location + "\\" + bulkExportId + ".zip";
                }



                if (bulkExport == null)
                {
                    string message = $"Could not retrieve bulkexport with id \"{bulkExportId}\".";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";

                    continue; //if we could not retrieve this bulkexport we continue with the next bulkexport
                }

                if (bulkExport.State == BulkExportState.Busy && ApplicationSettings.WaitForBulkexportToFinish)
                {
                    int WaitTimeInHours = ApplicationSettings.WaitTimeForExportToBeReadyInHours;

                    for (int i = 0; i < (WaitTimeInHours * 12); i++)
                    {
                        bulkExport = ReadBulkExportFromApi(bulkExportId);
                        if (bulkExport.State != BulkExportState.Busy)
                        {
                            break;
                        }

                        Logging.WriteToLog("Bulkexport is busy, waiting for 5 minutes to check again");

                        Thread.Sleep((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                    }
                }

                if (!bulkExport.can_download)
                {
                    //send e-mailmessage that bulk export settings are not correct for download
                    string message = $"The bulkexport \"{bulkExport.name}\" is not configured to allow download (manually or via the API).";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";

                    continue;
                }


                emailBody += $@" - The Bulkexport with name ""{bulkExport.name}"" (""{bulkExport.bulk_export_id}"" has been found, with the status ""{bulkExport.State}({bulkExport.internalState})"").\n";

                var readyWithErrors = bulkExport.State == BulkExportState.ReadyWithErrors;
                var allowDownloadOfExportWithErrors = ApplicationSettings.AllowDownloadOfExportWithErrors;
                var canTryToDownload = (bulkExport.State == BulkExportState.Ready || (readyWithErrors && allowDownloadOfExportWithErrors));
                if (!canTryToDownload)
                {
                    string message = $@"The bulkexport ""{bulkExport.name}"" could not be downloaded, because the status of the bulkexport is ""{bulkExport.State}({bulkExport.internalState})"".";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";
                    continue;
                }

                // Log warning when trying to download bulk export ready, but with errors (enabled with AllowDownloadOfExportWithErrors)
                if (readyWithErrors)
                {
                    string message = $"WARNING: Bulkexport \"{bulkExport.name}\" has been classified as \"Ready with errors\". \nThe tool will attempt to download the BulkExport, but be sure to verify why the errors have occurred and resolve them within the source environment (\"{ApplicationSettings.ZenyaURL}\").";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";
                }

                try
                {
                    if (ApplicationSettings.CleanUpPreviousExport)
                        DeleteBackups(bulkExportId, savePath);

                    BackupBulkExport(savePath, bulkExportPath);

                    //download new bulkexport, continue to next bulkexport if this fails. Errormessage is added to mail inside the DownloadBulkExport method

                    bool downloadSuccesful = restApi.DownloadBulkExport(bulkExportId, zipPath, bulkExport.name);

                    if (!downloadSuccesful)
                    {
                        var couldNotDownloadMessage = $"Could not download zip-file for bulkexport \"{bulkExport.name}\".";
                        Logging.WriteToLog(couldNotDownloadMessage);
                        emailBody += $" - {couldNotDownloadMessage}\n";

                        continue;
                    }

                    string message = $"Zip-file for bulkexport \"{bulkExport.name}\" has been downloaded into \"{zipPath}\".";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";

                    //extract new bulkexport
                    ExtractBulkExport(zipPath, bulkExportPath);

                    //delete backup if download en extract succeeds
                    if (ApplicationSettings.CleanUpPreviousExport)
                        DeleteBackups(bulkExportId, savePath);

                    if (!ApplicationSettings.SaveCopyOfZipFile)
                        DeleteZip(zipPath);
                }
                catch (Exception e)
                {
                    string message = $"ERROR: An error occurred when handling the bulkexport \"{bulkExport.name}\":\nMessage: {e.Message}\nStackTrace:\n{e.StackTrace}";
                    Logging.WriteToLog(message);
                    emailBody += $" - {message}\n";

                    continue;
                }
            }

            Logging.addMessageToEmailBody(emailBody);
            Logging.SendEmail();

            Console.WriteLine("done.");
            if (ApplicationSettings.DebugMode)
                Console.ReadLine();
        }

        static private BulkExport ReadBulkExportFromApi(string id)
        {
            Logging.WriteToLog($"Reading Bulk Export {id} from API.");

            RestResponse response = restApi.ReadBulkExport(id);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Logging.WriteToLog($"Downloading Bulk Export {id} from API.");

                string content = response.Content; // raw content as string
                var deserializedBulkExport = new BulkExport();
                System.IO.MemoryStream ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(deserializedBulkExport.GetType());

                Logging.WriteToLog($"Detected size of Bulk Export {id}: {ms.Length} bytes");

                deserializedBulkExport = ser.ReadObject(ms) as BulkExport;
                ms.Close();

                return deserializedBulkExport;
            }
            else
            {
                string message = $"Could not download zip-file, status code: {response.StatusCode}";
                if (response.ErrorMessage != null)
                {
                    message += ":\n " + response.ErrorMessage;
                }
                Logging.WriteToLog(message);
                emailBody += $" - {message}\n";

                return null;
            }

        }

        static private void DeleteBackups(string bulkExportId, string savePath)
        {
            Logging.WriteToLog($"Deleting backup folders for Bulk export \"{bulkExportId}\"");

            //delete old extract directories
            DirectoryInfo directoryInfo = new DirectoryInfo(savePath);
            DirectoryInfo[] backUpDirs = directoryInfo.GetDirectories(backUpDirPrefix + "*");
            if (backUpDirs.Length > 0)
            {
                foreach (DirectoryInfo dir in backUpDirs)
                {
                    try
                    {
                        Logging.WriteToLog($"- Deleting backup folder: {dir.FullName}");
                        dir.Delete(true);
                    }
                    catch (Exception e)
                    {
                        string message = $"ERROR: An error occurred when deleting the backup folder \"{dir.FullName}\":\nMessage: {e.Message}\nStackTrace:\n{e.StackTrace}";
                        Logging.WriteToLog(message);
                        emailBody += $" - {message}\n";
                    }
                }
            }
            else
            {
                Logging.WriteToLog($"No backup folders detected for Bulk export \"{bulkExportId}\". No action required.");
            }
        }

        static private void DeleteZip(string zipPath)
        {

            Logging.WriteToLog($"Deleting zip file: \"{zipPath}\"");
            FileInfo zipInfo = new FileInfo(zipPath);

            try
            {
                zipInfo.Delete();
            }
            catch (Exception e)
            {
                string message = $"ERROR: An error occurred when deleting the zip file \"{zipPath}\":\nMessage: {e.Message}\nStackTrace:\n{e.StackTrace}";
                Logging.WriteToLog(message);
                emailBody += $" - {message}\n";
            }

        }


        private static void BackupBulkExport(string savePath, string bulkExportPath)
        {
            Logging.WriteToLog($"Verifying if a Backup can be made for Bulk Export: {bulkExportPath}");
            if (Directory.Exists(savePath) && Directory.Exists(bulkExportPath))
            {
                Logging.WriteToLog($"Creating backup for: {savePath}");

                string backUpDir = backUpDirPrefix + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                Directory.CreateDirectory(Path.Combine(savePath, backUpDir));

                foreach (string dirPath in Directory.GetDirectories(bulkExportPath, "*", SearchOption.AllDirectories))
                {
                    //Create the subdirectorys
                    Directory.CreateDirectory(dirPath.Replace(bulkExportPath, Path.Combine(savePath, backUpDir)));
                    
                    //Move files to the subdirectories in the backup folder
                    foreach (string filepath in Directory.GetFiles(dirPath))
                    {
                        string fileName = Path.GetFileName(filepath);
                        string targetFilePath = filepath.Replace(bulkExportPath, Path.Combine(savePath, backUpDir));
                        File.Move(filepath, targetFilePath);
                    }
                }
                //Move the files in the root directory
                foreach (string filepath in Directory.GetFiles(bulkExportPath))
                {
                    string fileName = Path.GetFileName(filepath);
                    string targetFilePath = Path.Combine(savePath, backUpDir, fileName);
                    File.Move(filepath, targetFilePath);
                }
                emailBody += $" - Backup \"{backUpDir}\" has been created in: {savePath}\n";
            }
            else
            {
                Logging.WriteToLog($"Bulk Export folder \"{savePath}\" does not exist. No action required.");
            }
        }

        static private void ExtractBulkExport(string zipPath, string bulkExportPath)
        {
            Logging.WriteToLog($"Extracting zip-file (\"{zipPath}\") to: {bulkExportPath}");
            if (!Directory.Exists(bulkExportPath))
            {
                Directory.CreateDirectory(bulkExportPath);
            }
            else
            {
                Logging.WriteToLog($"Cleaning up the current Bulk Export folder \"{bulkExportPath}\".");
                DirectoryInfo bulkExportDir = new DirectoryInfo(bulkExportPath);
                CleanUpFolder(bulkExportDir);
            }

            // .NET 4.8 does not support the "overwriteFiles"-parameter. This is why we need to clear the folder before extracting the Zipfile into the Bulk export folder.
            ZipFile.ExtractToDirectory(zipPath, bulkExportPath);
            emailBody += $" - Zip-file (\"{zipPath}\") has been extracted to: {bulkExportPath}\n";
        }

        static private void CleanUpFolder(DirectoryInfo dirInfo)
        {
            // only log the following line, if the "DebugMode" has been set to true.
            if (ApplicationSettings.DebugMode)
                Logging.WriteToLog($"Cleaning up files from folder \"{dirInfo.FullName}\".");

            // delete all files from the root of the Bulk export folder
            foreach (FileInfo file in dirInfo.GetFiles())
            {

                var retriesLeft = 3;
                while (retriesLeft > 0)
                {
                    try
                    {
                        file.Delete();
                        break; // success!
                    }
                    catch (IOException e)
                    {
                        if (--retriesLeft == 0)
                        {
                            Logging.WriteToLog($"Error deleting file \"{file.FullName}\". No more retries left.\nException: {e.Message}\n{e.StackTrace}");
                            throw;
                        }
                        Thread.Sleep(1000);
                    }
                }

            }
            // delete all the subfolders from the Bulk export folder
            foreach (DirectoryInfo subdirInfo in dirInfo.GetDirectories())
            {
                CleanUpFolder(subdirInfo);
                subdirInfo.Delete();
            }
        }

    }
}