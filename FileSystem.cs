using System.IO;
using System.IO.Compression;

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
    }
}
