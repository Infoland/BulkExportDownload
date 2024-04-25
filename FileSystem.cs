using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BulkExportDownload
{
    public class FileSystem
    {
        public bool IsZipValid(string path)
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

        public enum InnerZipState
        {
            NoFiles,
            MoreThanOneFile,
            Success,
            NoZipFound,
            UnzippedButCouldNotDeleteZip
        }

        public (bool Success, string Message) DeleteZip(string zipPath)
        {

            Logging.WriteToLog($"Deleting zip file: \"{zipPath}\"");
            FileInfo zipInfo = new FileInfo(zipPath);

            try
            {
                zipInfo.Delete();
                return (true, null);
            }
            catch (Exception e)
            {
                string message = $"ERROR: An error occurred when deleting the zip file \"{zipPath}\":\nMessage: {e.Message}\nStackTrace:\n{e.StackTrace}";
                Logging.WriteToLog(message);
                return (false, message);
            }

        }

        public InnerZipState ExtractInnerZipFileWhenPresent(string bulkExportPath)
        {
            var files = Directory.GetFiles(bulkExportPath);
            switch (files.Length)
            {
                case 0:
                    return InnerZipState.NoFiles;
                case 1:
                    var file = files.Single();
                    if (!file.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                        return InnerZipState.NoZipFound;

                    ZipFile.ExtractToDirectory(file, bulkExportPath);

                    var deleteZipResult = DeleteZip(file);
                    if(!deleteZipResult.Success)
                        return InnerZipState.NoZipFound;

                    return InnerZipState.Success;
                default: return InnerZipState.MoreThanOneFile;
            }
        }
    }
}
