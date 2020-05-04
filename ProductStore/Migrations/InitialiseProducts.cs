using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using VideoStore.Models;

namespace VideoStore.Migrations
{
    public static class InitialiseSamples
    {
        public static void go()
        {
            const String partitionName = "Video_Partition";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureStorage"].ToString());
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("Videos");

            // If table doesn't already exist in storage then create and populate it with some initial values, otherwise do nothing
            if (!table.Exists())
            {
                // Create table if it doesn't exist already
                table.CreateIfNotExists();

                // Create the batch operation.
                TableBatchOperation batchOperation = new TableBatchOperation();

                // Create a video entity and add it to the table.
                VideoEntity video1 = new VideoEntity(partitionName, "1");
                video1.Title = "bee.mp4";
                video1.CreatedDate = DateTime.Now;

                // Create another video entity and add it to the table.
                VideoEntity video2 = new VideoEntity(partitionName, "2");
                video2.Title = "bunny.mp4";
                video2.CreatedDate = DateTime.Now;


                // Create another video entity and add it to the table.
                VideoEntity video3 = new VideoEntity(partitionName, "3");
                video3.Title = "giraffes.mp4";
                video3.CreatedDate = DateTime.Now;
                //video3.SampleDate = null;


                // Create another video entity and add it to the table.
                VideoEntity video4 = new VideoEntity(partitionName, "4");
                video4.Title = "ostrich.mp4";
                video4.CreatedDate = DateTime.Now;


                // Create another video entity and add it to the table.
                VideoEntity video5 = new VideoEntity(partitionName, "5");
                video5.Title = "sintel.mp4";
                video5.CreatedDate = DateTime.Now;


                // Create another video entity and add it to the table.
                VideoEntity video6 = new VideoEntity(partitionName, "6");
                video6.Title = "swann.mp4";
                video6.CreatedDate = DateTime.Now;

                // Create another video entity and add it to the table.
                VideoEntity video7 = new VideoEntity(partitionName, "7");
                video7.Title = "swann.mp4";
                video7.CreatedDate = DateTime.Now;


                // Add video entities to the batch insert operation.
                batchOperation.Insert(video1);
                batchOperation.Insert(video2);
                batchOperation.Insert(video3);
                batchOperation.Insert(video4);
                batchOperation.Insert(video5);
                batchOperation.Insert(video6);
                batchOperation.Insert(video7);

                // Execute the batch operation.
                table.ExecuteBatch(batchOperation);
            }

        }
    }
}