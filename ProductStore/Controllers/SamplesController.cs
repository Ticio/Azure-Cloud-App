using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using VideoStore.Models;
//using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace VideoStore.Controllers
{
    public class SamplesController : ApiController
    {
        private const String partitionName = "Video_Partition";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;

        private BlobStorageService _blobStorageService = new BlobStorageService();

        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Videos");
        }

        /// <summary>
        /// Get all Samples
        /// </summary>
        /// <returns></returns>
        // GET: api/Samples
        public IEnumerable<Sample> Get()
        {
            TableQuery<VideoEntity> query = new TableQuery<VideoEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<VideoEntity> entityList = new List<VideoEntity>(table.ExecuteQuery(query));

            // Basically create a list of Product from the list of ProductEntity with a 1:1 object relationship, filtering data as needed
            IEnumerable<Sample> productList = from e in entityList
                                               select new Sample()
                                               {
                                                   SampleID = e.RowKey,
                                                   Title = e.Title,
                                                   SampleMp4Blob = e.SampleMp4Blob,
                                                   SampleMp4URL = e.SampleMp4URL,
                                                   Mp4Blob = e.Mp4Blob,
                                                   CreatedDate = e.CreatedDate,
                                                   SampleDate = e.SampleDate
                                               };
            return productList;
        }

        // GET: api/Samples/5
        /// <summary>
        /// Get a video
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetProduct(string id)
        {
            // Create a retrieve operation that takes a product entity.
            TableOperation getOperation = TableOperation.Retrieve<VideoEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                VideoEntity productEntity = (VideoEntity)getOperationResult.Result;
                Sample p = new Sample()
                {
                    SampleID = productEntity.RowKey,
                    Title = productEntity.Title,
                    SampleMp4Blob = productEntity.SampleMp4Blob,
                    SampleMp4URL = productEntity.SampleMp4URL,
                    Mp4Blob = productEntity.Mp4Blob,
                    CreatedDate = productEntity.CreatedDate,
                    SampleDate = productEntity.SampleDate
            };
                return Ok(p);
            }
        }

        // POST: api/Samples
        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostProduct(Sample sample)
        {
            //creating a new instance of VideoEntity and initializing it
            VideoEntity sampleEntity = new VideoEntity()
            {
                RowKey = getNewMaxRowKeyValue(),
                PartitionKey = partitionName,
                Title = sample.Title,
                SampleMp4Blob = sample.SampleMp4Blob,
                SampleMp4URL = sample.SampleMp4URL,
                Mp4Blob = sample.Mp4Blob,
                CreatedDate = sample.CreatedDate,
                SampleDate = sample.SampleDate
        };

            // Create the TableOperation that inserts the sample entity.
            var insertOperation = TableOperation.Insert(sampleEntity);

            // Execute the insert operation.
            table.Execute(insertOperation);

            //return Ok(sample);

            return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sampleEntity);
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a product
        /// </summary>
        /// <param name="id"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutProduct(string id, Sample sample)
        {
            //Validating the request by analyzing url id and sample id
            if (id != sample.SampleID){
                return BadRequest();
            }

            // Create a retrieve operation that takes a product entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<VideoEntity>(partitionName, id);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            VideoEntity updateEntity = (VideoEntity)retrievedResult.Result;

            deleteOldBlob(updateEntity);

            updateEntity.Title = sample.Title;
            updateEntity.SampleMp4Blob = sample.SampleMp4Blob;
            updateEntity.SampleMp4URL = sample.SampleMp4URL;
            updateEntity.Mp4Blob = sample.Mp4Blob;
            updateEntity.CreatedDate = sample.CreatedDate;
            updateEntity.SampleDate = sample.SampleDate;

            // Create the TableOperation that inserts the product entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }
        
        // DELETE: api/Samples/5
        /// <summary>
        /// Delete a video
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteProduct(string id)
        {

            // Create a retrieve operation that takes a product entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<VideoEntity>(partitionName, id);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            

            if (retrievedResult.Result == null) return NotFound();
            else
            {
                VideoEntity updateEntity = (VideoEntity)retrievedResult.Result;
                deleteOldBlob(updateEntity);
                TableOperation deleteOperation = TableOperation.Delete(updateEntity);
                table.Execute(deleteOperation);

                return Ok(retrievedResult.Result);
            }
        }

        //Function to delete a blob used for update and delete requests
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

        //Getting max row key value function
        private String getNewMaxRowKeyValue()
        {
            TableQuery<VideoEntity> query = new TableQuery<VideoEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (VideoEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }
    }
}
