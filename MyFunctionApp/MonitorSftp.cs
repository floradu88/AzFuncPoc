using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

public class MonitorSftpFunction(BlobServiceClient blobServiceClient, Func<string, Task<string>> getSecretAsync)
{
    private readonly HttpClient _httpClient = new();

    [Function("MonitorSftpFunction")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var logger = context.GetLogger("MonitorSftpFunction");

        logger.LogInformation($"Timer trigger function executed at: {DateTime.Now}");

        // Retrieve secrets lazily from Key Vault
        string host = await getSecretAsync("SftpHost");
        string username = await getSecretAsync("SftpUsername");
        string password = await getSecretAsync("SftpPassword");
        string blobContainerName = await getSecretAsync("BlobContainerName");
        string malwareScanApiEndpoint = await getSecretAsync("MalwareScanApiEndpoint");
        string malwareScanApiKey = await getSecretAsync("MalwareScanApiKey");

        using var sftp = new SftpClient(host, username, password);
        try
        {
            sftp.Connect();
            logger.LogInformation("Connected to SFTP server");

            var files = sftp.ListDirectory("/");
            foreach (var file in files)
            {
                if (!file.IsDirectory && !file.IsSymbolicLink)
                {
                        logger.LogInformation($"Processing file: {file.Name} - Last Modified: {file.LastWriteTime}");

                    // Download file from SFTP
                    using var fileStream = new MemoryStream();
                    sftp.DownloadFile(file.FullName, fileStream);
                    fileStream.Position = 0;

                    // Upload to Azure Blob Storage
                    var blobContainer = blobServiceClient.GetBlobContainerClient(blobContainerName);
                    var blobClient = blobContainer.GetBlobClient(file.Name);
                    await blobClient.UploadAsync(fileStream, overwrite: true);

                    // Malware scanning
                    logger.LogInformation($"Scanning blob '{file.Name}' for malware...");
                    fileStream.Position = 0;

                    using var content = new StreamContent(fileStream);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malwareScanApiKey);

                    var response = await _httpClient.PostAsync(malwareScanApiEndpoint, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        logger.LogInformation($"Malware Scan Result: {result}");
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        logger.LogError($"Error scanning blob: {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing files: {ex.Message}");
        }
        finally
        {
            sftp.Disconnect();
            logger.LogInformation("Disconnected from SFTP server");
        }
    }
}
