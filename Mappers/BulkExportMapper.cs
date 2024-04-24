using BulkExportDownload.Dtos;

namespace BulkExportDownload.Mappers
{
    public class BulkExportMapper
    {
        /// <summary>
        /// Map a string to a BulkExportState
        /// </summary>
        /// <param name="input">String from the api</param>
        /// <returns>State</returns>
        /// <remarks>Made it as robust as possible to be able to handle changes in the API TT67679</remarks>
        public BulkExportState Map(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return BulkExportState.Unknown;

            input = input.ToLower().Replace("_", "").Trim();

            switch (input)
            {
                case "busy":
                    return BulkExportState.Busy;
                case "ready":
                    return BulkExportState.Ready;
                case "readywitherrors":
                    return BulkExportState.ReadyWithErrors;
                default:
                    return BulkExportState.Unknown;
            }
        }
    }
}
