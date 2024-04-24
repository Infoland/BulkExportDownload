using RestSharp;
using System;
using System.IO;
using System.Threading;

namespace BulkExportDownload
{
    public class RestAPI
    {
        private FileSystem _fileSystem;

        public RestAPI()
        {
            _fileSystem = new FileSystem();
        }
        public RestResponse ReadBulkExport(string bulkexportid)
        {
            var request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}", Method.Get);
            request.AddUrlSegment("bulk_export_id", bulkexportid); // replaces matching token in request.Resource

            var client = GetAuthenticatedRestClient();
            var response = client.Execute(request);

            return response;
        }

        private RestClient GetAuthenticatedRestClient()
        {
            var token = GetToken();
            var client = new RestClient(ApplicationSettings.ZenyaURL); ;
            client.AddDefaultHeader("Authorization", $"token {token}");
            client.AddDefaultHeader("x-api-version", "5");
            client.AddDefaultHeader("x-api_key", ApplicationSettings.ApiKey);

            return client;

        }

        /// <summary>
        /// Get authentication token from Zenya
        /// </summary>
        /// <returns></returns>
        private string GetToken()
        {
            var request = new RestRequest("api/tokens", Method.Post);
            request.AddJsonBody(new { api_key = ApplicationSettings.ApiKey, username = ApplicationSettings.UserName });

            var client = new RestClient(ApplicationSettings.ZenyaURL);
            RestResponse response = client.Execute(request);

            string tokenID = "";
            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != "")
                tokenID = response.Content.Replace("\"", "");

            return tokenID;
        }

        public bool DownloadBulkExport(string id, string zipPath, string bulkExportName)
        {
            bool blnSuccess = true;
            int tryCount = ApplicationSettings.DownloadTries;

            try
            {
                // verify if zipPath exists.
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(zipPath)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(zipPath));
                    Logging.WriteToLog($"Folder \"{System.IO.Path.GetDirectoryName(zipPath)}\" has been created for bulkexport {id}");
                }
            }
            catch
            {
                Logging.WriteToLog($"Could not create folder \"{zipPath}\" for bulkexport {id}");
            }

            while (tryCount > 0)
            {
                try
                {
                    DownloadZIP(tryCount, id, zipPath);
                    break; // successfully downloaded export
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog($"Downloading export failed with message {ex.Message}");

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


        private void DownloadZIP(int triesLeft, string bulkExportId, string zipPath)
        {
            Logging.WriteToLog($"Downloading zip-file for bulkexport {bulkExportId}, tries left {triesLeft}");

            var client = GetAuthenticatedRestClient();

            //Download the ZIP file
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {

                RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}/download", Method.Get);
                request.AddUrlSegment("bulk_export_id", bulkExportId); // replaces matching token in request.Resource


                client.DownloadStream(request).CopyTo(fileStream);
            }

            if (!_fileSystem.IsZipValid(zipPath))
                throw new Exception("ZIP file is not valid");

            Logging.WriteToLog($"Bulkexport has been downloaded to {System.IO.Path.GetDirectoryName(zipPath)} and is valid");

        }
    }

}
