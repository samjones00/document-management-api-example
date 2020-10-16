using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentManager.Common.Commands;
using DocumentManager.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using DocumentManager.Common.Validators;

namespace DocumentManager.Api
{
    public class AzureFunctions
    {
        //private readonly IUploadItemFactory _uploadItemFactory;
        //private readonly CosmosClient _cosmosClient;
        //private readonly CloudBlobClient _cloudBlobClient;
        private readonly IMediator _mediator;

        //public AzureFunctions(IUploadItemFactory uploadItemFactory, IConfiguration configuration)
        public AzureFunctions(IMediator mediator)
        {
            _mediator = mediator;
            //_uploadItemFactory = uploadItemFactory;
            //var cosmosConnectionString = configuration.GetConnectionString(Constants.Cosmos.ConnectionStringName);
            //_cosmosClient = new CosmosClient(cosmosConnectionString);

            //var storageConnectionString = configuration.GetConnectionString(Constants.Storage.ConnectionStringName);

            //CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount);
            //_cloudBlobClient = storageAccount.CreateCloudBlobClient();
        }

        [FunctionName(nameof(Upload))]
        public async Task<IActionResult> Upload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest httpRequest,
            ILogger log)
        {
            var sw = new Stopwatch();
            sw.Start();

            var uploadRequest = JsonConvert.DeserializeObject<UploadRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            var filename = uploadRequest.Filename;
            var byteArray = Convert.FromBase64String(uploadRequest.Data);

            uploadRequest.Bytes = byteArray;

            var validator = new UploadRequestValidator();

            var validationResult = validator.Validate(uploadRequest);

            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.Errors.Select(e => new {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                }));
            }

            log.LogInformation($"Uploading {filename} to blob storage...");

            await _mediator.Send(new UploadFileCommand(filename, byteArray));

            log.LogInformation($"Upload of {filename} completed in {sw.ElapsedMilliseconds}ms.");

            return new CreatedResult("/", new
            {
                Filename = filename,
                Size = byteArray.Length
            });
        }

        [FunctionName(nameof(CreateRecord))]
        public async Task CreateRecord([BlobTrigger("uploads/{name}", Connection = "")]
            Stream stream, string name,
            ILogger log)
        {
            //var uploadItem = _uploadItemFactory.Create(name, stream.Length);

            //var container = _cosmosClient.GetContainer(Constants.Cosmos.DatabaseName, Constants.Cosmos.ContainerName);
            //await container.CreateItemAsync(uploadItem);

            await _mediator.Send(new CreateDocumentCommand(name, stream.Length));

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {stream.Length} Bytes");
        }

        [FunctionName(nameof(List))]
        public async Task<IActionResult> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            var documents = await _mediator.Send(new ListDocumentsQuery());

            //var container = _cosmosClient.GetContainer(Constants.Cosmos.DatabaseName, Constants.Cosmos.ContainerName);

            //var list = container.GetItemLinqQueryable<UploadItem>(true);

            if (documents == null || !documents.Any())
            {
                return new NotFoundResult();
            }

            log.LogInformation($"{documents.Count()} documents found.");

            return new OkObjectResult(documents);
        }

        [FunctionName(nameof(Delete))]
        public async Task<IActionResult> Delete(

            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete/{filename}")] HttpRequest req,
            string filename,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            //var uri = UriFactory.CreateDocumentUri(Constants.Cosmos.DatabaseName, Constants.Cosmos.ContainerName,
            //    filename);


            //var blobContainer = _cloudBlobClient.GetContainerReference(Constants.Storage.ContainerName);

            //var blob = blobContainer.GetBlockBlobReference(filename);

            //await blob.DeleteAsync();

            await _mediator.Send(new DeleteFileCommand(filename));

            await _mediator.Send(new DeleteDocumentCommand(filename));

            //var container = _cosmosClient.GetContainer(Constants.Cosmos.DatabaseName, Constants.Cosmos.ContainerName);
            //QueryDefinition queryDefinition = new QueryDefinition("select * from c");
            //var queryResultSetIterator = container.GetItemQueryIterator<UploadItem>(queryDefinition);

            //Microsoft.Azure.Cosmos.FeedResponse<UploadItem> currentResultSet =
            //    await queryResultSetIterator.ReadNextAsync();

            //var doc = currentResultSet.FirstOrDefault(x => x.Filename == filename);

            //await container.DeleteItemAsync<UploadItem>(doc.id, new PartitionKey(doc.ContentType));

            return new OkResult();
        }
    }
}
