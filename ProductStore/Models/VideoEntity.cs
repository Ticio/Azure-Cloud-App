// Entity class for Azure table
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace VideoStore.Models
{

    public class VideoEntity : TableEntity
    {
        public string Title { get; set; }
        public string SampleMp4Blob { get; set; }
        public string SampleMp4URL { get; set; }
        public string Mp4Blob { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? SampleDate { get; set; }

        public VideoEntity(string partitionKey, string SampleID)
        {
            PartitionKey = partitionKey;
            RowKey = SampleID;
        }

        public VideoEntity() { }

    }
}
