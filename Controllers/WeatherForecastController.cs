using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Diagnostics;
using Newtonsoft.Json;
using UploaderTest.Helpers;
using UploaderTest.Models;

namespace UploaderTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly VideoUploadQueue _videoUploadQueue;

        public WeatherForecastController(BlobServiceClient blobServiceClient, VideoUploadQueue videoUploadQueue)
        {
            _blobServiceClient = blobServiceClient;
            _videoUploadQueue = videoUploadQueue;
        }

        [HttpGet]
        public string Get()
        {
            var exists = _blobServiceClient.GetBlobContainerClient("test").Exists().Value;
            if (!exists)
            {
                var res = _blobServiceClient.CreateBlobContainer("test");
            }
            

            var testData = System.IO.File.OpenRead("C:\\Data\\TestData.txt");

            BlobClient c = _blobServiceClient.GetBlobContainerClient("test").GetBlobClient("test");

            c.Upload(testData, overwrite: true);

            return _blobServiceClient.GetBlobContainerClient("test").GetBlobClient("test").DownloadContent().Value.Content.ToString();
        }

        [HttpPost("Video")]
        [RequestSizeLimit(100_000_000)]
        public async Task<ActionResult<string>> UploadVideo(IFormFile video)
        {
            var containerExists = await _blobServiceClient.GetBlobContainerClient("test").ExistsAsync();
            if (!containerExists.Value)
            {
                var add = await _blobServiceClient.CreateBlobContainerAsync("test");
            }

            var blobContainer = _blobServiceClient.GetBlobContainerClient("test");
            var blob = blobContainer.GetBlobClient(Guid.NewGuid().ToString() + ".mp4");
            var res = await blob.UploadAsync(video.OpenReadStream());

            var url = blob.Uri.AbsoluteUri;

            Media m = new Media
            {
                Url = url,
                Status = UploadStatus.PreUpload
            };
 
            _videoUploadQueue.Enqueue(m);
            
            return Ok();
        }


    }
}