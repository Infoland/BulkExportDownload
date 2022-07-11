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

namespace BulkExportDownload
{
    class RestAPI
    {
        // private string credentials = "credentials u:" + (Properties.Settings.Default.Username) + " " + "pwd:" + (Properties.Settings.Default.Password);
        static RestClient client = new RestClient(Properties.Settings.Default.Url);


        static public IRestResponse readBulkExport(string bulkexportid)
        {
            RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}", Method.GET);
            request.AddUrlSegment("bulk_export_id", bulkexportid); // replaces matching token in request.Resource

            // easily add HTTP Headers
            request.AddHeader("Authorization", string.Format("token {0}", getToken()));
            request.AddHeader("x-api-version", "1");
            request.AddHeader("x-api_key", Properties.Settings.Default.ApiKey);

            IRestResponse response = client.Execute(request);

            return response;
        }

        static private string getToken()
        {
            string tokenID = "";

            RestRequest request = new RestRequest("api/tokens", Method.POST);

            jsonRequestData requestData = new jsonRequestData();
            requestData.api_key = Properties.Settings.Default.ApiKey;
            requestData.username = Properties.Settings.Default.Username;    

            request.AddJsonBody(requestData);

            IRestResponse response = client.Execute(request);

            if(response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != "")
                tokenID = response.Content.Replace("\"", "");

            return tokenID;
        }

        static public bool downloadBulkExport(string id, string zipPath, string bulkExportName)
        {
            bool blnSuccess = false;

            
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

            Logging.writeToLog(String.Format("Downloading zip-file for bulkexport {0}", id));
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {
                RestRequest request = new RestRequest("api/documents/bulk_exports/{bulk_export_id}/download", Method.GET)
                {
                    ResponseWriter = (responseStream) => responseStream.CopyTo(fileStream)
                };
                request.AddUrlSegment("bulk_export_id", id); // replaces matching token in request.Resource

                // easily add HTTP Headers
                request.AddHeader("Authorization", string.Format("token {0}", getToken()));
                request.AddHeader("x-api-version", "1");
                request.AddHeader("x-api_key", Properties.Settings.Default.ApiKey);

                client.DownloadData(request);

                if (fileStream.Length > 0)
                    blnSuccess = true;
                else
                    blnSuccess = false;
            }

            return blnSuccess;
        }


    }

    class jsonRequestData
    {
        public string api_key;
        public string username;
    }

}
