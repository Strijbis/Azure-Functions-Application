using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Azure.Storage.Queues.Models;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

using BuienradarResponseFormat;
using ArtInstituteOfChicagoResponseFormat;
using Azure.Storage.Sas;

namespace FunctionApp {
    public class QueueMessageExternal
    {
        public required string clientID { get; set; }
        public required string weatherData { get; set; }
        public required string imageURL { get; set; }
    }

    public class  MultiResponse
    {
        [QueueOutput("client-queue")]
        public required string QueueMessage { get; set; }

        [HttpResult]
        public required IActionResult Response { get; set; }
    }

    public class ClientToQueue
    {
        private readonly ILogger<ClientToQueue> _logger;

        public ClientToQueue(ILogger<ClientToQueue> logger)
        {
            _logger = logger;
        }

        [Function("ClientToQueue")]
        public MultiResponse Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Triggered HttpTrigger function ClientToQueue!");

            string clientID = Guid.NewGuid().ToString();
            _logger.LogInformation($"Generated the following Client ID: {clientID}");

            return new MultiResponse
            {
                QueueMessage = clientID,
                Response = new OkObjectResult($"Your ID is: {clientID}")
            };
        }
    }

    public class QueueToExternalQueue
    {
        private readonly ILogger<QueueToExternalQueue> _logger;

        public QueueToExternalQueue(ILogger<QueueToExternalQueue> logger)
        {
            _logger = logger;
        }

        [Function("QueueToExternalQueue")]
        [QueueOutput("external-queue")]
        public async Task<QueueMessageExternal[]> Run([QueueTrigger("client-queue")] QueueMessage queueMessage)
        {
            _logger.LogInformation("Triggered QueueTrigger function QueueToExternalQueue!");

            // Connect to the Blob Service and prepare the Blob Container
            var blobServiceClient = new BlobServiceClient(
                new Uri("https://ssp581663st.blob.core.windows.net"),
                new DefaultAzureCredential());

            string containerName = queueMessage.Body.ToString();
            //_logger.LogInformation($"Creating Blob Storage Container for Client ID: {containerName}");
            //BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName, PublicAccessType.Blob);
            _logger.LogInformation("Connecting to Blob Service Client.");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob).Wait();

            // The Art Institute of Chicago's API throws a 403 Forbidden when no User-Agent is present.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C# Azure Functions Application");

            _logger.LogInformation("Sending request to Buienradar.");
            var responseBuienradar = await httpClient.GetFromJsonAsync<BuienradarResponse>("https://data.buienradar.nl/2.0/feed/json");
            _logger.LogInformation("Received request from Buienradar.");

            // Take the region name and temperature from three regions.
            int[] regionNr = { 13, 14, 15 };
            string[] regionName = new string[regionNr.Length];
            double[] temperature = new double[regionNr.Length];
            for (int i = 0; i < regionNr.Length; i++)
            {
                regionName[i] = responseBuienradar.actual.stationmeasurements.ElementAt(regionNr[i]).regio;
                temperature[i] = responseBuienradar.actual.stationmeasurements.ElementAt(regionNr[i]).temperature;
            }

            // The name of the first region is used in the search term to the Art Institute of Chicago API.
            _logger.LogInformation("Sending request to the Art Institute of Chicago.");
            var responseArtInstituteOfChicago = await httpClient.GetFromJsonAsync<ArtInstituteOfChicagoResponse>($"https://api.artic.edu/api/v1/artworks/search?q={regionName[0]}&fields=id,title,image_id");
            _logger.LogInformation("Received request from the Art Institute of Chicago.");

            // The API does not directly link to the image, instead the image url is constructed from the iiif_url and image_id.
            string iiifURL = responseArtInstituteOfChicago.config.iiif_url;
            string imageID = responseArtInstituteOfChicago.data.ElementAt(0).image_id;
            string imageURL = ArtInstituteOfChicago.GetImageUrl(iiifURL, imageID);

            // Fill the queue messages
            QueueMessageExternal[] queueMessages = new QueueMessageExternal[regionNr.Length];
            for (int i = 0; i < regionNr.Length; i++)
            {
                queueMessages[i] = new QueueMessageExternal()
                {
                    clientID = queueMessage.Body.ToString(),
                    weatherData = $"{regionName[i]}: {temperature[i]}",
                    imageURL = imageURL
                };
            }

            return queueMessages;
        }
    }

    public class ExternalQueueToBlob {
        private readonly ILogger<ExternalQueueToBlob> _logger;

        public ExternalQueueToBlob(ILogger<ExternalQueueToBlob> logger) {
            _logger = logger;
        }

        [Function("ExternalQueueToBlob")]
        public async Task Run([QueueTrigger("external-queue")] QueueMessage queueMessage) {
            _logger.LogInformation("Triggered QueueTrigger function ExternalQueueToBlob!");

            var blobServiceClient = new BlobServiceClient(
                new Uri("https://ssp581663st.blob.core.windows.net"),
                new DefaultAzureCredential());

            // All letters in a container name must be lowercase.
            // Create a Blob Container with the Client ID as name, it'll store the processed images.
            QueueMessageExternal queueMessageExternal = queueMessage.Body.ToObjectFromJson<QueueMessageExternal>();
            string containerName = queueMessageExternal.clientID;

            _logger.LogInformation("Connecting to Blob Service Client.");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // The Art Institute of Chicago's API throws a 403 Forbidden when no User-Agent is present.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "C# Azure Functions Application");

            // Request the image from the Art Institute of Chicago's API.
            string imageURL = queueMessageExternal.imageURL;
            _logger.LogInformation("Sending request to the Art Institute of Chicago.");
            var responseImageStream = await httpClient.GetStreamAsync(imageURL);
            _logger.LogInformation("Received request from the Art Institute of Chicago.");

            _logger.LogInformation("Loading image from response.");
            var image = await Image.LoadAsync(responseImageStream);
            _logger.LogInformation("Finished loading image from response.");

            // Prepare all options for SixLabors ImageSharp.
            Font font = SystemFonts.CreateFont("Verdana", 22);
            PatternBrush brush = Brushes.Horizontal(Color.White, Color.White);
            PatternPen pen = Pens.DashDot(Color.Black, 1);
            RichTextOptions options = new RichTextOptions(font)
            {
                Origin = new PointF(10, 10),
                TabWidth = 8,
                WrappingLength = 2000,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Edit the image
            string imageText = queueMessageExternal.weatherData;
            _logger.LogInformation("Editing image.");
            image.Mutate(x => x.DrawText(options, imageText, brush, pen));
            _logger.LogInformation("Finished editing image.");

            // Save the image to a stream
            Stream imageStream = new MemoryStream();
            string fileName = Guid.NewGuid().ToString() + ".png";
            _logger.LogInformation("Saving image as stream.");
            await image.SaveAsPngAsync(imageStream);
            _logger.LogInformation("Finished saving image as stream.");
            
            // Reset the position of the stream back to the beginning
            imageStream.Position = 0;

            // Get a reference to the Blob that will store the image
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            _logger.LogInformation("Uploading image to Blob Storage");
            await blobClient.UploadAsync(imageStream, true);
            _logger.LogInformation("Finished uploading image to Blob Storage");
        }
    }

    public class ClientToBlobToClient
    {
        private readonly ILogger<ClientToBlobToClient> _logger;

        public ClientToBlobToClient(ILogger<ClientToBlobToClient> logger)
        {
            _logger = logger;
        }

        [Function("ClientToBlobToClient")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Triggered HttpTrigger function ClientToBlobToClient!");

            var blobServiceClient = new BlobServiceClient(
                new Uri("https://ssp581663st.blob.core.windows.net"),
                new DefaultAzureCredential());

            string? containerName = req.Query["id"];
            if (containerName is null)
            {
                return new BadRequestObjectResult("No ID was provided!");
            } else if (containerName.Length < 3)
            {
                return new BadRequestObjectResult("Requested ID does not meet the minimum length (3) for Blob Containers!");
            }

            _logger.LogInformation("Connecting to Blob Client.");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (containerClient.Exists())
            {
                _logger.LogInformation($"Connecting to Blob Storage Container for Client ID: {containerName}");
            } else
            {
                _logger.LogInformation($"Blob Storage Container does not exist for Client ID: {containerName}");
                return new BadRequestObjectResult($"No Blob Storage Container found for ID: {containerName}");
            }

            var credentials = new ManagedIdentityCredential();
            var sasExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30);
            var userDelegationkey = await blobServiceClient.GetUserDelegationKeyAsync(null, sasExpiryTime, CancellationToken.None);

            string output = "";
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);

                var sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerName,
                    BlobName = blobItem.Name,
                    ExpiresOn = sasExpiryTime
                };
                sasBuilder.SetPermissions(BlobAccountSasPermissions.Read);
                var sasToken = sasBuilder.ToSasQueryParameters(userDelegationkey, "ssp581663st");

                output += $"{blobClient.Uri}?{sasToken}\n";
            }

            return new OkObjectResult($"Found the following images:\n{output}");
        }
    }
}