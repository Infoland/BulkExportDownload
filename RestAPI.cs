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
        static RestClient client = new RestClient(Properties.Settings.Default.Url);

        static public RestResponse readBulkExport(string bulkexportid)
        {
            RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}", Method.Get);
            request.AddUrlSegment("bulk_export_id", bulkexportid); // replaces matching token in request.Resource

            // easily add HTTP Headers
            request.AddHeader("Authorization", string.Format("token {0}", getToken()));
            request.AddHeader("x-api_key", Properties.Settings.Default.ApiKey);

            RestResponse response = client.Execute(request);

            return response;
        }

        //Get authentication token from Zenya
        static private string getToken()
        {
            string tokenID = "";

            RestRequest request = new RestRequest("api/tokens", Method.Post);

            request.AddJsonBody(new { api_key = Properties.Settings.Default.ApiKey, username = Properties.Settings.Default.Username });

            RestResponse response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != "")
                tokenID = response.Content.Replace("\"", "");

            return tokenID;
        }

        static public bool downloadBulkExport(string id, string zipPath, string bulkExportName)
        {
            bool blnSuccess = true;
            int trycount = Properties.Settings.Default.DownloadTries;

            try
            {
                // verify if zipPath exists.
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(zipPath)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(zipPath));
                    Logging.writeToLog(String.Format("Folder \"{0}\" has been created for bulkexport {1}", System.IO.Path.GetDirectoryName(zipPath), id));
                }
            }
            catch
            {
                Logging.writeToLog(String.Format("Could not create folder \"{0}\" for bulkexport {1}", zipPath, id));
            }

            while (trycount > 0)
            {
                try
                {
                    downloadZIP(trycount, id, zipPath, bulkExportName);
                    break; // successfully downloaded export
                }
                catch (Exception ex)
                {
                    Logging.writeToLog(String.Format("Downloading export failed with message {0}", ex.Message));

                    if (--trycount == 0)
                        return false;

                    //Wait for 5 minutes to try again
                    Thread.Sleep((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                }
            }
            return blnSuccess;
        }


        private static void downloadZIP(int triesLeft, string id, string zipPath, string bulkExportName)
        {
            Logging.writeToLog(String.Format("Downloading zip-file for bulkexport {0}, tries left {1}", id, triesLeft));

            //Download the ZIP file
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {

                RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}/download", Method.Get);
                request.AddUrlSegment("bulk_export_id", id); // replaces matching token in request.Resource

                request.AddHeader("Authorization", string.Format("token {0}", getToken()));
                request.AddHeader("x-api_key", Properties.Settings.Default.ApiKey);

                client.DownloadStream(request).CopyTo(fileStream);
            }

            //Check if downloaded ZIP is valid
            if (IsZipValid(zipPath))
            {
                Logging.writeToLog(String.Format("Bulkexport has been downloaded to {0} and is valid", System.IO.Path.GetDirectoryName(zipPath), id));
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
