using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;


namespace BulkExportDownload
{
    class Program
    {
        static internal string emailBody;
        static internal List<SaveLocation> BulkExportsToDownload = new List<SaveLocation>();
        const string backUpDirPrefix = "back_up_";

        static void Main(string[] args)
        {
            Logging.writeToLog("Reading Bulk Export Downloader Tool settings");

            //populate list of SaveLocation with the BulkExportsToDownload setting
            string[] saveLocs = Properties.Settings.Default.BulkExportsToDownload.Split(';');

            Logging.writeToLog(String.Format("Bulk export(s) to download: {0}", saveLocs.Length));

            foreach (string saveLoc in saveLocs)
            {
                string[] splitLoc = saveLoc.Split(',');
                string id = splitLoc[0].Trim();
                string savePath = splitLoc[1].Trim();
                SaveLocation saveLocation = new SaveLocation(id, savePath);
                BulkExportsToDownload.Add(saveLocation);
            }

            Logging.writeToLog(String.Format("Setting API URL to: {0}", Properties.Settings.Default.Url));

            emailBody = String.Format("At {0}, the Bulk Export Downloader has verified the state of the given Bulk Export(s) for \"{1}\" and the following steps have been executed:\n", DateTime.Now.ToShortDateString(), Properties.Settings.Default.Url);

            foreach (SaveLocation saveLoc in BulkExportsToDownload)
            {
                string bulkExportId = saveLoc.id;
                string savePath = saveLoc.location;
                string bulkExportPath;
                string zipPath;

                BulkExport bulkExport = readBulkExportFromApi(bulkExportId);

                if (Properties.Settings.Default.UseBulkexportNameAsFoldername == "True")
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
                    string message = String.Format("Could not retrieve bulkexport with id \"{0}\".", bulkExportId);
                    Logging.writeToLog(message);
                    emailBody += String.Format(" - {0}\n", message);
                    
                    continue; //if we could not retrieve this bulkexport we continue with the next bulkexport
                }

                if (bulkExport.state == "busy" && Properties.Settings.Default["WaitForBulkexportToFinish"].ToString() == "True")
                {
                    int WaitTimeInHours = (int)Properties.Settings.Default["WaitTimeForExportToBeReadyInHours"];

                    for (int i = 0; i < (WaitTimeInHours * 12); i++)
                    {
                        bulkExport = readBulkExportFromApi(bulkExportId);
                        if (bulkExport.state != "busy")
                        {
                            break;
                        }

                        Logging.writeToLog("Bulkexport is busy, waiting for 5 minutes to check again");

                        Thread.Sleep((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                    }
                }

                if (!bulkExport.can_download)
                {
                    //send e-mailmessage that bulk export settings are not correct for download
                    string message = String.Format("The bulkexport \"{0}\" is not configured to allow download (manually or via the API).", bulkExport.name);
                    Logging.writeToLog(message);
                    emailBody += String.Format(" - {0}\n", message);

                    continue;
                }

                bool AllowDownloadOfExportWithErrors = bool.Parse(Properties.Settings.Default.AllowDownloadOfExportWithErrors);

                emailBody += String.Format(" - The Bulkexport with name \"{0}\" (\"{1}\" has been found, with the status \"{2}\").\n", bulkExport.name, bulkExport.bulk_export_id, bulkExport.state);

                if (bulkExport.state == "ready" || (bulkExport.state == "readyWithErrors" && AllowDownloadOfExportWithErrors))
                {
                    try
                    {
                        if (bulkExport.state == "readyWithErrors")
                        {
                            string message = String.Format("WARNING: Bulkexport \"{0}\" has been classified as \"Ready with errors\". \nThe tool will attempt to download the BulkExport, but be sure to verify why the errors have occurred and resolve them within the source environment (\"{1}\").", bulkExport.name, Properties.Settings.Default.Url);
                            Logging.writeToLog(message);
                            emailBody += String.Format(" - {0}\n", message);
                        }

                        if (bool.Parse(Properties.Settings.Default.CleanUpPreviousExport))
                            DeleteBackups(bulkExportId, savePath);

                        BackupBulkExport(savePath, bulkExportPath);

                        //download new bulkexport, continue to next bulkexport if this fails. Errormessage is added to mail inside the DownloadBulkExport method

                        bool downloadSuccesful = RestAPI.downloadBulkExport(bulkExportId, zipPath, bulkExport.name);

                        if (!downloadSuccesful)
                        {
                            string message = String.Format("Could not download zip-file for bulkexport \"{0}\".", bulkExport.name);
                            Logging.writeToLog(message);
                            emailBody += String.Format(" - {0}\n", message);

                            continue;
                        }
                        else
                        {
                            string message = String.Format("Zip-file for bulkexport \"{0}\" has been downloaded into \"{1}\".", bulkExport.name, zipPath);
                            Logging.writeToLog(message);
                            emailBody += String.Format(" - {0}\n", message);
                        }

                        if (downloadSuccesful) { 
                            //extract new bulkexport
                            ExtractBulkExport(zipPath, bulkExportPath);
                        }

                        //delete backup if download en extract succeeds
                        if (bool.Parse(Properties.Settings.Default.CleanUpPreviousExport))
                            DeleteBackups(bulkExportId, savePath);

                        if(!bool.Parse(Properties.Settings.Default.SaveCopyOfZipFile))
                            DeleteZip(zipPath);
                    }
                    catch (Exception e)
                    {
                        string message = String.Format("ERROR: An error occurred when handling the bulkexport \"{0}\":\nMessage: {1}\nStackTrace:\n{2}", bulkExport.name, e.Message, e.StackTrace);
                        Logging.writeToLog(message);
                        emailBody += String.Format(" - {0}\n", message);

                        continue;
                    }
                }
                else
                {
                    string message = String.Format("The bulkexport \"{0}\" could not be downloaded, because the status of the bulkexport is \"{1}\".", bulkExport.name, bulkExport.state);
                    Logging.writeToLog(message);
                    emailBody += String.Format(" - {0}\n", message);
                }
            }

            Logging.addMessageToEmailBody(emailBody);
            Logging.SendEmail();

            Console.WriteLine("done.");
            if (Properties.Settings.Default.DebugMode)
                Console.ReadLine();
        }

        static private BulkExport readBulkExportFromApi(string id)
        {
            Logging.writeToLog(String.Format("Reading Bulk Export {0} from API.", id));

            RestResponse response = RestAPI.readBulkExport(id);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Logging.writeToLog(String.Format("Downloading Bulk Export {0} from API.", id));

                string content = response.Content; // raw content as string
                BulkExport deserializedBulkExport = new BulkExport();
                System.IO.MemoryStream ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(deserializedBulkExport.GetType());

                Logging.writeToLog(String.Format("Detected size of Bulk Export {0}: {1} bytes", id, ms.Length));

                deserializedBulkExport = ser.ReadObject(ms) as BulkExport;
                ms.Close();
                
                return deserializedBulkExport;
            }
            else
            {
                string message = String.Format("Could not download zip-file, status code: {0}", response.StatusCode);
                if (response.ErrorMessage != null)
                {
                    message += ":\n " + response.ErrorMessage;
                }
                Logging.writeToLog(message);
                emailBody += String.Format(" - {0}\n", message);

                return null;
            }

        }

        static private void DeleteBackups(string bulkExportId, string savePath)
        {
            Logging.writeToLog(String.Format("Deleting backup folders for Bulk export \"{0}\"", bulkExportId));

            //delete old extract directories
            DirectoryInfo directoryInfo = new DirectoryInfo(savePath);
            DirectoryInfo[] backUpDirs = directoryInfo.GetDirectories(backUpDirPrefix + "*");
            if(backUpDirs.Length > 0)
            { 
                foreach (DirectoryInfo dir in backUpDirs)
                {
                    try
                    {
                        Logging.writeToLog(String.Format("- Deleting backup folder: {0}", dir.FullName));
                        dir.Delete(true);
                    }
                    catch (Exception e)
                    {
                        string message = String.Format("ERROR: An error occurred when deleting the backup folder \"{0}\":\nMessage: {1}\nStackTrace:\n{2}", dir.FullName, e.Message, e.StackTrace);
                        Logging.writeToLog(message);
                        emailBody += String.Format(" - {0}\n", message);
                    }
                }
            }
            else
            {
                Logging.writeToLog(String.Format("No backup folders detected for Bulk export \"{0}\". No action required.", bulkExportId));
            }
        }

        static private void DeleteZip(string zipPath)
        {

            Logging.writeToLog(String.Format("Deleting zip file: \"{0}\"", zipPath));
            FileInfo zipInfo = new FileInfo(zipPath);

            try
            {
                zipInfo.Delete();
            }
            catch (Exception e)
            {
                string message = String.Format("ERROR: An error occurred when deleting the zip file \"{0}\":\nMessage: {1}\nStackTrace:\n{2}", zipPath, e.Message, e.StackTrace);
                Logging.writeToLog(message);
                emailBody += String.Format(" - {0}\n", message);
            }

        }


        private static void BackupBulkExport(string savePath, string bulkExportPath)
        {
            Logging.writeToLog(String.Format("Verifying if a Backup can be made for Bulk Export: {0}", bulkExportPath));
            if (Directory.Exists(savePath) && Directory.Exists(bulkExportPath))
            {
                Logging.writeToLog(String.Format("Creating backup for: {0}", savePath));

                string backUpDir = backUpDirPrefix + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                Directory.CreateDirectory(Path.Combine(savePath, backUpDir));

                foreach (string dirPath in Directory.GetDirectories(bulkExportPath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(bulkExportPath, Path.Combine(savePath, backUpDir)));
                }

                foreach (string filepath in Directory.GetFiles(bulkExportPath))
                {
                    string fileName = Path.GetFileName(filepath);
                    string targetFilePath = Path.Combine(savePath, backUpDir, fileName);
                    File.Move(filepath, targetFilePath);
                }
                emailBody += String.Format(" - Backup \"{0}\" has been created in: {1}\n", backUpDir, savePath);
            }
            else
            {
                Logging.writeToLog(String.Format("Bulk Export folder \"{0}\" does not exist. No action required.", savePath));
            }
        }

        static private void ExtractBulkExport(string zipPath, string bulkExportPath)
        {
            Logging.writeToLog(String.Format("Extracting zip-file (\"{0}\") to: {1}", zipPath, bulkExportPath));
            if (!Directory.Exists(bulkExportPath))
            {
                Directory.CreateDirectory(bulkExportPath);
            }
            else
            {
                Logging.writeToLog(String.Format("Cleaning up the current Bulk Export folder \"{0}\".", bulkExportPath));
                DirectoryInfo bulkExportDir = new DirectoryInfo(bulkExportPath);
                CleanUpFolder(bulkExportDir);
            }

            // .NET 4.8 does not support the "overwriteFiles"-parameter. This is why we need to clear the folder before extracting the Zipfile into the Bulk export folder.
            ZipFile.ExtractToDirectory(zipPath, bulkExportPath);
            emailBody += String.Format(" - Zip-file (\"{0}\") has been extracted to: {1}\n", zipPath, bulkExportPath);
        }

        static private void CleanUpFolder(DirectoryInfo dirInfo)
        {   
            // only log the following line, if the "DebugMode" has been set to true.
            if (Properties.Settings.Default.DebugMode)
                Logging.writeToLog(String.Format("Cleaning up files from folder \"{0}\".", dirInfo.FullName));

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
                            Logging.writeToLog(String.Format("Error deleting file \"{0}\". No more retries left.\nException: {1}\n{2}", file.FullName, e.Message, e.StackTrace));
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

    [DataContract]
    internal class BulkExport
    {
        [DataMember]
        internal Int32 bulk_export_id;

        [DataMember]
        internal string name;

        [DataMember]
        internal string state;

        [DataMember]
        internal string last_export_datetime;

        [DataMember]
        internal bool can_download;
    }

}


