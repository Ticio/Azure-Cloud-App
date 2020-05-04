using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using VideoStore.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace VideoStore.Controllers
{
    public class DataController : ApiController
    {

        private const String partitionName = "Video_Partition";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;

        private BlobStorageService _blobStorageService = new BlobStorageService();
        private CloudQueueService _queueStorageService = new CloudQueueService();

        public DataController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Videos");
        }

        // GET: api/Data
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Data/5
        [ResponseType(typeof(Sample))]
        public IHttpActionResult Get(string id)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<VideoEntity>(partitionName, id);
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Construct response including a new DTO as apprporiatte
            if (retrievedResult.Result == null) {
                return NotFound();
            }else{

                CloudBlobContainer blobContainer = BlobStorageService.getCloudBlobContainer();
                //CloudBlockBlob blob = blobContainer.GetBlockBlobReference(sample.SampleMp4Blob);

                VideoEntity sample = (VideoEntity)retrievedResult.Result;

                // Setting up a video after being PUT and then create it Blob
                var sampleBlob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("origin/" + sample.Mp4Blob);
                Stream blobStream = sampleBlob.OpenRead();

                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
                message.Content = new StreamContent(blobStream);
                message.Content.Headers.ContentLength = sampleBlob.Properties.Length;
                message.Content.Headers.ContentType = new
                System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
                message.Content.Headers.ContentDisposition = new
                System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = sampleBlob.Name,
                    Size = sampleBlob.Properties.Length
                };
                return ResponseMessage(message);
            }
        }

        // PUT: api/Data/5
        [ResponseType(typeof(Sample))]
        public IHttpActionResult Put(string id)
        {
            // Create a retrieve operation that takes a music entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<VideoEntity>(partitionName, id);

            // Execute the retrieve operation.
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Videos");
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Construct response including a new DTO as apprporiatte
            if (retrievedResult.Result == null){
                return NotFound();
            }else
            {
                VideoEntity sampleEntity = (VideoEntity)retrievedResult.Result;
                deleteOldBlob(sampleEntity);

                String mp4BlobName = string.Format("{0}{1}", Guid.NewGuid(), ".mp4");

                var mp4Blob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("origin/" + mp4BlobName);
                var request = HttpContext.Current.Request;
                mp4Blob.Properties.ContentType = "video/mp4";

                mp4Blob.UploadFromStream(request.InputStream);
                mp4Blob.SetMetadata();

                var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
                String sampleURL = baseUrl.ToString() + "/api/data/" + id;
                sampleEntity.Mp4Blob = mp4BlobName;
                sampleEntity.SampleMp4URL = sampleURL;
                sampleEntity.SampleMp4Blob = null;

                TableOperation updateOperation = TableOperation.InsertOrReplace(sampleEntity);
                // Execute the insert operation.
                table.Execute(updateOperation);

                var queueMessageSample = new VideoEntity(partitionName, id);
                _queueStorageService.getCloudQueue().AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(queueMessageSample)));
                   
                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        private void deleteOldBlob(VideoEntity sampleEntity)
        {
            var updateOperation = TableOperation.InsertOrReplace(sampleEntity);

            if (sampleEntity.Mp4Blob != null || sampleEntity.SampleMp4Blob != null || sampleEntity.SampleMp4URL != null || sampleEntity.SampleDate != null)
            {   //Setting the value null before setting new values
                sampleEntity.Mp4Blob = null;
                sampleEntity.SampleMp4Blob = null;
                sampleEntity.SampleMp4URL = null;
                // Execute the insert operation.
                table.Execute(updateOperation);

                var mp4Blob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("origin/" + sampleEntity.Mp4Blob);
                var videoBlob = BlobStorageService.getCloudBlobContainer().GetBlockBlobReference("updated/" + sampleEntity.Mp4Blob);
                mp4Blob.DeleteIfExists();
                videoBlob.DeleteIfExists();
            }
        }
    }
}
