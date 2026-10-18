using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

public class BlobMoveHelper
{
    private readonly BlobContainerClient _sourceContainer;
    private readonly BlobContainerClient _destinationContainer;

    private readonly BlobContainerClient _maliciousContainer;
    private readonly ILogger _logger;

    public BlobMoveHelper(string storageConnectionString, string sourceContainerName, string destinationContainerName, string maliciousContainerName, ILogger logger)
    {
        _sourceContainer = new BlobContainerClient(storageConnectionString, sourceContainerName);
        _destinationContainer = new BlobContainerClient(storageConnectionString, destinationContainerName);
        _maliciousContainer = new BlobContainerClient(storageConnectionString, maliciousContainerName);

        _logger = logger;
    }

    /// <summary>
    /// Moves all files from one container to another.
    /// </summary>
    public async Task MoveFilesAsync()
    {
        await _destinationContainer.CreateIfNotExistsAsync();

        // List all blobs in source container
        await foreach (BlobItem blobItem in _sourceContainer.GetBlobsAsync())
        {
            string blobName = blobItem.Name;
            var sourceBlob = _sourceContainer.GetBlobClient(blobName);
            var destinationBlob = _destinationContainer.GetBlobClient(blobName);
            var maliciousBlob = _maliciousContainer.GetBlobClient(blobName);

            Console.WriteLine($"Moving file: {blobName}");

            var tags = await sourceBlob.GetTagsAsync();

            if (tags.Value.Tags.ContainsKey("Malware Scanning scan result") && tags.Value.Tags["Malware Scanning scan result"] == "Malicious")
            {
                _logger.LogInformation($"Found a malicious file and moving it to its folder {sourceBlob.Uri}");
                await maliciousBlob.StartCopyFromUriAsync(sourceBlob.Uri);
            }
            else
            {
                // Start copy operation
                await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

                // Ensure copy completes before deleting original
                BlobProperties properties;
                do
                {
                    await Task.Delay(500); // Wait for copy to complete
                    properties = await destinationBlob.GetPropertiesAsync();
                }
                while (properties.CopyStatus == Azure.Storage.Blobs.Models.CopyStatus.Pending);

                if (properties.CopyStatus == Azure.Storage.Blobs.Models.CopyStatus.Success)
                {
                    // Delete original blob
                    await sourceBlob.DeleteAsync();
                    Console.WriteLine($"Successfully moved: {blobName}");
                }
                else
                {
                    Console.WriteLine($"Failed to copy {blobName}: {properties.CopyStatusDescription}");
                }
            }
        }
    }
}
