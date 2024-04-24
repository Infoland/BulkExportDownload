using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;
using System.Threading;

namespace BulkExportDownload
{
    class RestAPI
    {
        // private string credentials = "credentials u:" + (Properties.Settings.Default.Username) + " " + "pwd:" + (Properties.Settings.Default.Password);
        static RestClient client = new RestClient(ApplicationSettings.ZenyaURL);

        static public RestResponse readBulkExport(string bulkexportid)
        {
            RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}", Method.Get);
            request.AddUrlSegment("bulk_export_id", bulkexportid); // replaces matching token in request.Resource

            // easily add HTTP Headers
            request.AddHeader("Authorization", $"token {getToken()}");
            request.AddHeader("x-api-version", "5");
            request.AddHeader("x-api_key", ApplicationSettings.ApiKey);

            RestResponse response = client.Execute(request);

            return response;
        }

        //Get authentication token from Zenya
        static private string getToken()
        {
            string tokenID = "";

            RestRequest request = new RestRequest("api/tokens", Method.Post);

            request.AddJsonBody(new { api_key = ApplicationSettings.ApiKey, username = ApplicationSettings.UserName });

            RestResponse response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != "")
                tokenID = response.Content.Replace("\"", "");

            return tokenID;
        }

        static public bool DownloadBulkExport(string id, string zipPath, string bulkExportName)
        {
            bool blnSuccess = true;
            int tryCount = ApplicationSettings.DownloadTries;

            try
            {
                // verify if zipPath exists.
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(zipPath)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(zipPath));
                    Logging.writeToLog($"Folder \"{System.IO.Path.GetDirectoryName(zipPath)}\" has been created for bulkexport {id}");
                }
            }
            catch
            {
                Logging.writeToLog($"Could not create folder \"{zipPath}\" for bulkexport {id}");
            }

            while (tryCount > 0)
            {
                try
                {
                    DownloadZIP(tryCount, id, zipPath, bulkExportName);
                    break; // successfully downloaded export
                }
                catch (Exception ex)
                {
                    Logging.writeToLog($"Downloading export failed with message {ex.Message}");

                    //Substract one from the count
                    tryCount -= 1;

                    if (tryCount == 0)
                        return false;

                    //Wait for 5 minutes to try again
                    Thread.Sleep((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                }
            }
            return blnSuccess;
        }


        private static void DownloadZIP(int triesLeft, string id, string zipPath, string bulkExportName)
        {
            Logging.writeToLog($"Downloading zip-file for bulkexport {id}, tries left {triesLeft}");

            //Download the ZIP file
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {

                RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}/download", Method.Get);
                request.AddUrlSegment("bulk_export_id", id); // replaces matching token in request.Resource

                request.AddHeader("Authorization", $"token {getToken()}");
                request.AddHeader("x-api-version", "5");
                request.AddHeader("x-api_key", ApplicationSettings.DownloadTries);

                client.DownloadStream(request).CopyTo(fileStream);
            }

            //Check if downloaded ZIP is valid
            if (IsZipValid(zipPath))
            {
                Logging.writeToLog($"Bulkexport has been downloaded to {System.IO.Path.GetDirectoryName(zipPath)} and is valid");
            }
            else
            {
                throw new Exception("ZIP file is not valid");
            }

        }

        public static bool IsZipValid(string path)
        {
            try
            {
                using (var zipFile = ZipFile.OpenRead(path))
                {
                    var entries = zipFile.Entries;
                    return true;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

    }

}
