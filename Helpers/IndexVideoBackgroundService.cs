using Azure.Storage.Blobs.Models;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using UploaderTest.Context;
using UploaderTest.Models;
using Microsoft.EntityFrameworkCore;

namespace UploaderTest.Helpers
{
    public class IndexVideoBackgroundService: BackgroundService
    {
        private readonly VideoUploadQueue _queue;
        private readonly IServiceProvider _serviceProvider;

        private readonly string _primaryKey = "";
        private readonly string _secondaryKey = "";
        private readonly string _accountId = "";
        private readonly string _location = "trial";
        private readonly string _url = "https://api.videoindexer.ai";

        public IndexVideoBackgroundService(VideoUploadQueue queue, IServiceProvider serviceProvider)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Media? video = _queue.Dequeue();
                if (video is not null)
                {
                    var scope = _serviceProvider.CreateAsyncScope();

                    await using (var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                    {

                        var handler = new HttpClientHandler();
                        handler.AllowAutoRedirect = false;
                        var client = new HttpClient(handler);
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _primaryKey);

                        // obtain account access token
                        var accountAccessTokenRequestResult = await client.GetAsync($"{_url}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true", stoppingToken);
                        var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                        client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                        var content = new MultipartFormDataContent();
                        Console.WriteLine("Uploading...");
                        // get the video from URL
                        // replace with the video URL
                        //Stream fs = video.OpenReadStream();
                        //byte[] data = new byte[fs.Length];
                        //fs.Read(data, 0, data.Length);
                        //content.Add(new ByteArrayContent(data), "My Video", "My Video");

                        var uploadRequestResult = await client.PostAsync($"{_url}/{_location}/Accounts/{_accountId}/Videos?accessToken={accountAccessToken}&name=some_name&description=some_description&privacy=private&partition=some_partition&videoUrl={video.Url}", content, stoppingToken);
                        var uploadResult = await uploadRequestResult.Content.ReadAsStringAsync(stoppingToken);

                        string videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)?["id"] ?? "";
                        if (string.IsNullOrEmpty(videoId))
                        {
                            Console.WriteLine("Error uploading");
                            Console.WriteLine(uploadResult);
                            return;
                        }
                        video.Status = UploadStatus.Uploaded;

                        await _context.Media.AddAsync(video, stoppingToken);
                        await _context.SaveChangesAsync(stoppingToken);

                        Console.WriteLine("Uploaded");
                        Console.WriteLine(uploadResult);

                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _primaryKey);
                        var videoTokenRequestResult = client.GetAsync($"{_url}/auth/{_location}/Accounts/{_accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
                        var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                        video.Status = UploadStatus.Processing;
                        await _context.SaveChangesAsync(stoppingToken);

                        while (true)
                        {
                            Thread.Sleep(10000);

                            var videoGetIndexRequestResult = client.GetAsync($"{_url}/{_location}/Accounts/{_accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                            var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                            string processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)?["state"] ?? "";

                            Console.WriteLine("");
                            Console.WriteLine("State:");
                            Console.WriteLine(processingState);

                            // job is finished
                            if (processingState != "Uploaded" && processingState != "Processing")
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Full JSON:");
                                Console.WriteLine(videoGetIndexResult);
                                video.Status = UploadStatus.Processed;
                                video.Insights = videoGetIndexResult;
                                await _context.SaveChangesAsync(stoppingToken);
                                break;
                            }
                        }
                    }

                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }


  
            }
        }
    }
}
