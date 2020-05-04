// This is a Data Transfer Object (DTO) class. This is sent/received in REST requests/responses.
// Read about DTOS here: https://docs.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5

using System;
using System.ComponentModel.DataAnnotations;

namespace VideoStore.Models
{
    public class Sample
    {
        /// <summary>
        /// Product ID
        /// </summary>
        [Key]
        public string SampleID { get; set; }

        /// <summary>
        /// Title of the sample
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// SampleMp4Blob of the sample
        /// </summary>
        public string SampleMp4Blob { get; set; }

        /// <summary>
        /// SampleMp4URL of the sample
        /// </summary>
        public string SampleMp4URL { get; set; }

        /// <summary>
        /// Mp4Blob of the sample
        /// </summary>
        public string Mp4Blob { get; set; }

        /// <summary>
        /// CreatedDate of the sample
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// SampleDate of the sample
        /// </summary>
        public DateTime? SampleDate { get; set; }
    }
}