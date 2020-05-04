using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using NReco.VideoConverter;
using VideoStore.Models;
using VideoStore;
using System.Configuration;


namespace VideoSampleWebJob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage(
         [QueueTrigger("videomaker")] VideoEntity videoInQueue,
         [Table("Videos", "{PartitionKey}", "{RowKey}")] VideoEntity videoInTable,
         [Table("Videos")] CloudTable tableBinding, TextWriter logger)
        {

            CloudStorageAccount storageAccount;
            CloudTableClient tableClient;
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            tableBinding = tableClient.GetTableReference("Videos");

            // Create a retrieve operation that takes a video entity.
            TableOperation getOperation = TableOperation.Retrieve<VideoEntity>("Video_Partition", videoInQueue.PartitionKey);

            // Execute the retrieve operation.
            TableResult getOperationResult = tableBinding.Execute(getOperation);
            videoInTable = (VideoEntity)getOperationResult.Result;

            logger.WriteLine("Video Generated started...");

            var inputBlob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("origin/" + videoInTable.Mp4Blob);
            inputBlob.FetchAttributes();
            String videoBlobName = string.Format("{0}{1}", Guid.NewGuid(), ".mp4");

            var outputBlob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("updated/" + videoBlobName);

            using (Stream input = inputBlob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                ConvertVideoToPreviewMP4(input, output, 20);
                outputBlob.Properties.ContentType = "video/mp4";
                if (inputBlob.Metadata.ContainsKey("Title"))
                {
                    outputBlob.Metadata["Title"] = inputBlob.Metadata["Title"];
                    videoInTable.Title = inputBlob.Metadata["Title"];
                }
                else
                {
                    outputBlob.Metadata["Title"] = " ";
                }
            }

            videoInTable.SampleMp4Blob = videoBlobName;
            videoInTable.SampleDate = DateTime.Now;

            var updateOperation = TableOperation.InsertOrReplace(videoInTable);
            // Execute the insert operation.
            tableBinding.Execute(updateOperation);

            logger.WriteLine("GeneratePreview() finished...");
        }

        private static void ConvertVideoToPreviewMP4(Stream input, Stream output, int duration)
        {
            BinaryWriter Writer = null;
            try
            {
                // Create a new stream to write to the file
                Writer = new BinaryWriter(File.Open("temp.mp4", FileMode.Create));
                BinaryReader Reader = new BinaryReader(input);
                byte[] imageBytes = null;
                imageBytes = Reader.ReadBytes((int)input.Length);
                // Writer raw data                
                Writer.Write(imageBytes);
                Writer.Flush();
                Writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("* FileWrite exception: " + e.Message);

            }

            var vid_duration = new ConvertSettings();
            vid_duration.MaxDuration = duration;

            var ffMpeg = new FFMpegConverter();
            ffMpeg.ConvertMedia("temp.mp4", "mp4", output, "mp4", vid_duration);

        }
    }
}
