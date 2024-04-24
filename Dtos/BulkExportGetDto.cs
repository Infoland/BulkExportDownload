using System;
using System.Runtime.Serialization;

namespace BulkExportDownload.Dtos
{
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
