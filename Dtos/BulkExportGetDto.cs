using BulkExportDownload.Mappers;
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

        [DataMember(Name = "state")]
        internal string internalState;

        [DataMember]
        internal string last_export_datetime;

        [DataMember]
        internal bool can_download;

        public BulkExportState State
        {
            get
            {
                var mapper = new BulkExportMapper();
                return mapper.Map(internalState);
            }
        }
    }

    public enum BulkExportState
    {
        Busy,
        Ready,
        ReadyWithErrors,
        Unknown
    }
}


