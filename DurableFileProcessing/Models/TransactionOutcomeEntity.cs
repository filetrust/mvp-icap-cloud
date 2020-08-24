using Microsoft.Azure.Cosmos.Table;

namespace DurableFileProcessing.Models
{
    public class OutcomeEntity : TableEntity
    {
        public OutcomeEntity(string function, string hash, string filetype, string filestatus)
        {
            this.PartitionKey = function;
            this.RowKey = hash;
            this.FileType = filetype;
            this.FileStatus = filestatus;
        }

        public OutcomeEntity()
        {

        }

        public string FileType { get; set; }

        public string FileStatus { get; set; }
    }
}